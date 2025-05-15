using Newtonsoft.Json;
using pCFG = Genetic_Algorithm.Config;
using CFG = Genetic_Algorithm.Config.WorkerConfig;

namespace Genetic_Algorithm
{
    public class Worker {
        static List<Preferences> PREFERENCES_LIST { get; }

        static Worker() {
            if (File.Exists("testy")) {
                string list = File.ReadAllText("testy");
                PREFERENCES_LIST = JsonConvert.DeserializeObject<List<Preferences>>(list);
                if (PREFERENCES_LIST.Count != pCFG.WORKER_COUNT) File.Delete("testy");
            }
            if (!File.Exists("testy")) {
                PREFERENCES_LIST = new();
                for (int i = 0; i < pCFG.WORKER_COUNT; i++) {
                    PREFERENCES_LIST.Add(new());
                }
                string s = JsonConvert.SerializeObject(PREFERENCES_LIST);
                File.WriteAllText("testy", s);
            }
        }

        
        public int PreferenceIndex { get; init; }
        public bool[] AssignedShifts { get; set; }
        public double fitness { get; private set; }
        double[] WorkdayFavorabilities { get; set; }
        int ShiftCount { get; set; }
        int DislikedShiftCount { get; set; }
        int OffDayShiftCount { get; set; }
        Preferences PersonalPreference { get; set; }

        public Worker(int preferenceIndex) : this(preferenceIndex, GetRandomShifts()) { }

        public Worker(int preferenceIndex, bool[] assignedShifts) {
            if (preferenceIndex >= pCFG.WORKER_COUNT) throw new Exception("Invalid preferenceIndex, above worker limit");
            this.PreferenceIndex = preferenceIndex;
            this.PersonalPreference = PREFERENCES_LIST[preferenceIndex];
            this.AssignedShifts = assignedShifts;
            RecountShifts();
            WorkdayFavorabilities = new double[pCFG.WEEKDAYS];
            RecalculateFavorability();
        }

        
        static bool[] GetRandomShifts() {
            bool[] shifts = new bool[pCFG.WEEKDAYS];
            for (int day = 0; day < pCFG.WEEKDAYS; day++) {
                if (Program.Rand.NextDouble() < (double)(CFG.MAX_WORKDAYS) / pCFG.WEEKDAYS) {
                    shifts[day] = true;
                }
            }
            return shifts;
        }

        public bool AttemptMutation(int day, bool force = false) {
            double roll = Program.Rand.NextDouble();
            double threshold = (1 - WorkdayFavorabilities[day]) * CFG.MUTATION_CHANCE;
            bool rollSuccess =  roll > threshold; //można dać prosto do ifa, ale latwiejszy debug jest tym sposobem

            if (force || rollSuccess) {
                AssignedShifts[day] = !AssignedShifts[day];
                //okazja na poprawę: dodawanie lub odejmowanie z liczb zmian, nielubianych zmian itd zamiast przeliczania od nowa
                RecountShifts();
                RecalculateFavorability();
                return true;
            }
            return false;
        }

        public string ToString(int indent) {
            string message = new string(' ', 2*indent)+"Pracownik " + PreferenceIndex + ":\n";
            string indentString = new string(' ', 2 * indent + 2);
            message += indentString + "Dni przypisane: [" + String.Join(' ', AssignedShifts)+"]\n";
            message += indentString + "Dni nielubiane: [" + String.Join(' ', PersonalPreference.DislikedWorkdays) + "]\n";
            message += indentString + "Dni wolne     : [" + String.Join(' ', PersonalPreference.OffDays) + "]\n";
            message += indentString + "Chęć wymiany  : [" + String.Join(' ', WorkdayFavorabilities) + "]\n";
            message += indentString + "Fitness       : " + this.fitness;
            return message;
        } 

        //todo: zmiana z obliczania fitnessu - trzeba odejmować od jedynki gdyby to bylo przeciwne
        //w tej chwili, jeżeli są przypisane wolne dni to nie obniża to fitnessu tylko zwiększa odrobinę szanse mutacji
        void RecalculateFavorability() {
            this.fitness = 1;
            for (int day = 0; day < pCFG.WEEKDAYS; day++) {
                WorkdayFavorabilities[day] = FindFavorability(AssignedShifts[day], day);
                this.fitness += WorkdayFavorabilities[day] * (CFG.MAX_FITNESS -1) / pCFG.WEEKDAYS;
            }
        }

        //todo: zmiana musi wliczać wszystkie dni na których by to mialo efekt, z jakiegoś powodu mamy
        //1 dla nielubianego dnia wolnego 6 z false na true
        double FindMutationWeight(bool proposedShiftState, int day) {
            double weight = CFG.BASE_WEIGHT;
            double delta = (1-CFG.BASE_WEIGHT); //how much penalties matter
            int direction = proposedShiftState ? 1 : -1; //if there are penalties, we want them to increase the chance of unassigning
            weight -= getOverworkWeight();
            weight -= getDislikedWeight();
            weight -= getOffDayWeight();

            return weight;

            double getOverworkWeight() {
                return ShiftCountPenalty(CFG.MAX_WORKDAYS) //always apply, we use it to encourage assignment under 5 days
                * delta
                * direction
                * (isRiskingOverwork()? 1: -1)
                * CFG.OVERWORK_WEIGHT;
            }

            double getDislikedWeight() {
                bool dayIsDisliked = isDisliked(day);
                int retroactiveIncrease = (dayIsDisliked && !proposedShiftState) ? 1 : 0; //unless removing a disliked day, keep in mind the risk of an increased penalty
                return DislikedPenalty(DislikedShiftCount + retroactiveIncrease)
                    * delta
                    * direction
                    * (dayIsDisliked ? 1 : -1)
                    * CFG.DISLIKED_DAY_WEIGHT;
            }

            //almost identical to disliked days
            double getOffDayWeight() {
                bool dayIsOff = isOffDay(day);
                int retroactiveIncrease = (dayIsOff && !proposedShiftState) ? 1 : 0;
                return OffDayPenalty(OffDayShiftCount+retroactiveIncrease) //increasing if proposed shift state = true and is offday
                    * delta
                    * direction
                    * (dayIsOff? 1 : -1) 
                    * CFG.OFFDAY_WEIGHT;
            }
        }




        double OffDayPenalty(int count) {
            int max = PersonalPreference.OffDays.Length;
            if (max == 0) return 0;
            count = Math.Min(count, max);
            double penalty = count / max;
            return penalty;
        }
        bool isOffDay(int day) {
            return PersonalPreference.OffDays.Contains(day);
        }

        double ShiftCountPenalty(int count) { //in case i ever change formulas
            if (count > CFG.MAX_WORKDAYS) return 1;
            return 0;
        }
        bool isRiskingOverwork() {
            return ShiftCount >= CFG.MAX_WORKDAYS;
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
            for (int day = 0; day < pCFG.WEEKDAYS; day++) {
                bool isAssignedWork = AssignedShifts[day];
                if (isAssignedWork) {
                    ShiftCount++;
                    if (PersonalPreference.DislikedWorkdays.Contains(day)) DislikedShiftCount++;
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
                List<int> remainingDays = Enumerable.Range(0, pCFG.WEEKDAYS).ToList();
                int dislikedCount = 0;
                while (Program.Rand.NextDouble() < CFG.DISLIKED_CHANCE && dislikedCount < CFG.MAX_DISLIKED_DAYS) {
                    dislikedCount++;
                }
                DislikedWorkdays = new int[dislikedCount];
                remainingDays.Add(6); //niedziele mają wyższą szansę
                
                for(int i=0; i<dislikedCount; i++) {
                    DislikedWorkdays[i] = remainingDays[Program.Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == DislikedWorkdays[i]);
                }

            }

            void RandomizeOffdays() {
                int offdayCount = 0;
                List<int> remainingDays = Enumerable.Range(0, pCFG.WEEKDAYS).ToList();
                for (int i = 0; i < pCFG.WEEKDAYS; i++) {
                    if (Program.Rand.NextDouble() < CFG.OFFDAY_CHANCE) {
                        offdayCount++;
                    }
                }
                remainingDays.AddRange(DislikedWorkdays); //zmienia tylko wagi nielubianych dni
              
                this.OffDays = new int[offdayCount];
                for(int i=0; i<offdayCount; i++) {
                    OffDays[i] = remainingDays[Program.Rand.Next(remainingDays.Count)];
                    remainingDays.RemoveAll(day => day == OffDays[i]);
                }
            }
        }
    }
}