using Newtonsoft.Json;

namespace Genetic_Algorithm
{
    public class Worker {
        static Config CFG = Config.ConfigSingleton;
        static Config.WorkerConfig wCFG = CFG.wCFG;

        static List<Preferences> PREFERENCES_LIST { get; }

        static Worker() {
            if (File.Exists("testy")) {
                string list = File.ReadAllText("testy");
                PREFERENCES_LIST = JsonConvert.DeserializeObject<List<Preferences>>(list);
                if (PREFERENCES_LIST.Count != CFG.WORKER_COUNT) File.Delete("testy");
            }
            if (!File.Exists("testy")) {
                PREFERENCES_LIST = new();
                for (int i = 0; i < CFG.WORKER_COUNT; i++) {
                    PREFERENCES_LIST.Add(new());
                }
                string s = JsonConvert.SerializeObject(PREFERENCES_LIST);
                File.WriteAllText("testy", s);
            }
        }
        
        //todo: make assigned shifts, fitness, workday favorabilities and shift recounts lazy
        public int PreferenceIndex { get; init; }
        public bool[] AssignedShifts { get; set; }
        public double fitness { get; private set; }
        int ShiftCount { get; set; }
        int DislikedShiftCount { get; set; }
        int OffDayShiftCount { get; set; }
        Preferences PersonalPreference { get; set; }

        public Worker(int preferenceIndex) : this(preferenceIndex, GetRandomShifts()) { }

        public Worker(int preferenceIndex, bool[] assignedShifts) {
            if (preferenceIndex >= CFG.WORKER_COUNT) throw new Exception("Invalid preferenceIndex, above worker limit");
            this.PreferenceIndex = preferenceIndex;
            this.PersonalPreference = PREFERENCES_LIST[preferenceIndex];
            this.AssignedShifts = assignedShifts;
            RecountShifts();
            RecalculateFitness();
        }

        
        static bool[] GetRandomShifts() {
            bool[] shifts = new bool[Config.WEEKDAYS];
            for (int day = 0; day < Config.WEEKDAYS; day++) {
                if (Program.Rand.NextDouble() < (double)(wCFG.MAX_WORKDAYS) / Config.WEEKDAYS) {
                    shifts[day] = true;
                }
            }
            return shifts;
        }

        double?[] LazyMutationWeights = new double?[Config.WEEKDAYS];
        public bool AttemptMutation(int day, bool force = false) { //to change
            double threshold = LazyMutationWeights[day] ??= FindMutationWeight(day) * wCFG.MUTATION_CHANCE;
            double roll = Program.Rand.NextDouble();
            bool rollSuccess =  roll > threshold; //można dać prosto do ifa, ale latwiejszy debug jest tym sposobem

            if (force || rollSuccess) {
                AssignedShifts[day] = !AssignedShifts[day];
                int shiftChange = AssignedShifts[day] ? 1 : -1;
                ShiftCount += shiftChange;
                if (isDisliked(day)) DislikedShiftCount += shiftChange;
                if (isOffDay(day)) OffDayShiftCount += shiftChange;
                LazyMutationWeights[day] = FindMutationWeight(day);
                return true;
            }
            return false;
        }


        public string ToString(int indent) {
            RecalculateFitness();
            for(int day = 0; day<Config.WEEKDAYS; day++) {
                LazyMutationWeights[day] = FindMutationWeight(day);
            }
            string message = new string(' ', 2*indent)+"Pracownik " + PreferenceIndex + ":\n";
            string indentString = new string(' ', 2 * indent + 2);
            message += indentString + "Ilość dni przypisanych, nielubianych, wolnych: "+this.ShiftCount+", "+this.DislikedShiftCount+", " + this.OffDayShiftCount + "\n";
            message += indentString + "Dni przypisane: [" + String.Join(' ', AssignedShifts)+"]\n";
            message += indentString + "Dni nielubiane: [" + String.Join(' ', PersonalPreference.DislikedWorkdays) + "]\n";
            message += indentString + "Dni wolne     : [" + String.Join(' ', PersonalPreference.OffDays) + "]\n";
            message += indentString + "Chęć do pracy : [" + String.Join(' ', LazyMutationWeights) + "]\n";
            message += indentString + "Fitness       : " + this.fitness;
            return message;
        }


        public void RecalculateFitness() {
            fitness = 0;
            double overworkMalus = OverworkPenalty(ShiftCount) * wCFG.OVERWORK_WEIGHT;
            double dislikedMalus = DislikedPenalty(DislikedShiftCount) * wCFG.DISLIKED_DAY_WEIGHT;
            double offDayMalus = OffDayPenalty(OffDayShiftCount) * wCFG.OFFDAY_WEIGHT;
            for (int day = 0; day < Config.WEEKDAYS; day++) {
                fitness += 1
                    - overworkMalus
                    - (isDisliked(day) ? dislikedMalus: 0)
                    - (isOffDay(day) ?  offDayMalus: 0);
            }
            fitness = (fitness + 1) * (wCFG.MAX_FITNESS - 1) / Config.WEEKDAYS;
        }

        double FindMutationWeight(int day) {
            bool proposedShiftState = !AssignedShifts[day];
            double weight = wCFG.BASE_WEIGHT;
            double delta = (1-wCFG.BASE_WEIGHT); //how much penalties matter
            int direction = proposedShiftState ? 1 : -1; //if there are penalties, we want them to increase the chance of unassigning
            weight -= getOverworkWeight();
            weight -= getDislikedWeight();
            weight -= getOffDayWeight();

            return weight;

            double getOverworkWeight() {
                return OverworkPenalty(wCFG.MAX_WORKDAYS) //always apply, we use it to encourage assignment under 5 days
                * delta
                * direction
                * (isRiskingOverwork()? 1: -1)
                * wCFG.OVERWORK_WEIGHT;
            }

            double getDislikedWeight() {
                bool dayIsDisliked = isDisliked(day);
                int retroactiveIncrease = (dayIsDisliked && !proposedShiftState) ? 1 : 0; //unless removing a disliked day, keep in mind the risk of an increased penalty
                return DislikedPenalty(DislikedShiftCount + retroactiveIncrease)
                    * delta
                    * direction
                    * (dayIsDisliked ? 1 : -1)
                    * wCFG.DISLIKED_DAY_WEIGHT;
            }

            //almost identical to disliked days
            double getOffDayWeight() {
                bool dayIsOff = isOffDay(day);
                int retroactiveIncrease = (dayIsOff && !proposedShiftState) ? 1 : 0;
                return OffDayPenalty(OffDayShiftCount+retroactiveIncrease) //increasing if proposed shift state = true and is offday
                    * delta
                    * direction
                    * (dayIsOff? 1 : -1) 
                    * wCFG.OFFDAY_WEIGHT;
            }
        }




        double OffDayPenalty(int count) {
            int max = PersonalPreference.OffDays.Length;
            if (max == 0) return 0;
            count = Math.Min(count, max);
            double penalty = (double)count / max;
            return penalty;
        }
        bool isOffDay(int day) {
            return PersonalPreference.OffDays.Contains(day);
        }

        double OverworkPenalty(int count) { //in case i ever change formulas
            if (count > wCFG.MAX_WORKDAYS) return 1;
            return 0;
        }
        bool isRiskingOverwork() {
            return ShiftCount >= wCFG.MAX_WORKDAYS;
        }


        double DislikedPenalty(int count) {
            int max = PersonalPreference.DislikedWorkdays.Length;
            if (max == 0) return 0;

            count = Math.Min(count, max);
            double penalty = Math.Pow(8, ((double)count / max))/8;
            return penalty; 
        }
        bool isDisliked(int day) {
            return PersonalPreference.DislikedWorkdays.Contains(day);
        }

        void RecountShifts() {
            this.ShiftCount = 0;
            this.DislikedShiftCount = 0;
            this.OffDayShiftCount = 0;
            for (int day = 0; day < Config.WEEKDAYS; day++) {
                bool isAssignedWork = AssignedShifts[day];
                if (isAssignedWork) {
                    ShiftCount++;
                    if (isDisliked(day)) DislikedShiftCount++;
                    if (isOffDay(day)) OffDayShiftCount++;
                }
            }
        }
    

    private class Preferences {
            public int[] DislikedWorkdays { get; private set; }
            public int[] OffDays { get; private set; }

            public Preferences() {
                RandomizeDisliked();
                RandomizeOffdays();
            }

            void RandomizeDisliked() {
                List<int> remainingDays = Enumerable.Range(0, Config.WEEKDAYS).ToList();
                int dislikedCount = 0;
                while (Program.Rand.NextDouble() < wCFG.DISLIKED_CHANCE && dislikedCount < wCFG.MAX_DISLIKED_DAYS) {
                    dislikedCount++;
                }
                DislikedWorkdays = new int[dislikedCount];
                remainingDays.Add(6); //niedziele mają wyższą szansę
                
                for(int i=0; i<dislikedCount; i++) {
                    DislikedWorkdays[i] = remainingDays[Program.Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == DislikedWorkdays[i]);
                }

                DislikedWorkdays = DislikedWorkdays.Order().ToArray();

            }

            void RandomizeOffdays() {
                int offdayCount = 0;
                List<int> remainingDays = Enumerable.Range(0, Config.WEEKDAYS).ToList();
                for (int i = 0; i < Config.WEEKDAYS; i++) {
                    if (Program.Rand.NextDouble() < wCFG.OFFDAY_CHANCE) {
                        offdayCount++;
                    }
                }
                remainingDays.AddRange(DislikedWorkdays); //zmienia tylko wagi nielubianych dni
              
                this.OffDays = new int[offdayCount];
                for(int i=0; i<offdayCount; i++) {
                    OffDays[i] = remainingDays[Program.Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == OffDays[i]);
                }

                OffDays = OffDays.Order().ToArray();
            }
        }
    }
}