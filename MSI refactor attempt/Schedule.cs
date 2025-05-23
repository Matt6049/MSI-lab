namespace Genetic_Algorithm
{
    public class Schedule
    {
        static readonly int[] NEEDED_SHIFTS;
        static readonly Config CFG = Config.ConfigSingleton;
        static readonly Config.ScheduleConfig sCFG = CFG.sCFG;

        static Schedule() {
            NEEDED_SHIFTS = new int[Config.WEEKDAYS];
            for (int day=0; day< Config.WEEKDAYS; day++) {
                NEEDED_SHIFTS[day] = (int)Math.Ceiling(CFG.WORKER_COUNT * sCFG.SHIFT_PROPORTIONS[day]);
            }
        }


        Worker[] WorkersTable { get; set; }
        int[] CurrentShifts { get; set; }
        public double ScheduleFitness { get; private set; }
        public Schedule(Schedule[] parents) : this() {
            List<int> pointByPointTargets = RandomizeCrossoverTargets();
            CloneCrossover(parents, pointByPointTargets);
            PointByPointCrossover(parents, pointByPointTargets);
            //TODO: modyfikacja metod mutacyjnych, aby usunąć szansę nieskończoności prób
            //być może zmiana obliczeń fitnessu wedle feasibility
            RandomMutations();
            RecountShifts();
            this.ScheduleFitness = CalculateScheduleFitness();
        }

        public Schedule() {
            WorkersTable = new Worker[CFG.WORKER_COUNT];
            CurrentShifts = new int[Config.WEEKDAYS];
        }

        public void PrintShifts() {
            Console.WriteLine("Oczekiwane ilości zmian: [" + String.Join(' ', Schedule.NEEDED_SHIFTS) + "]");
            Console.WriteLine("Rzeczywiste zmiany     : [" + String.Join(' ', CurrentShifts) + "]");
        }

        public void PrintWorkerSchedules() {
            foreach (Worker worker in WorkersTable) {
                Console.WriteLine(worker.ToString(1));
            }
        }

        public string RequiredToString() {
            int workerCountPadding = (int)Math.Floor(Math.Log10(CFG.WORKER_COUNT-1)) + 2;
            string message = "Willingness to work\n" + new string(' ', workerCountPadding+2);
            for(int day=1; day<=Config.WEEKDAYS; day++) {
                message += ("D" + day).PadRight(4);
            }
            message = message.TrimEnd() + "\n";
            foreach(Worker worker in WorkersTable) {
                message += worker.WillingnessToString()+"\n";
            }
            message += "\nRequirements\n"+new string(' ', 4);

            for (int day = 1; day <= Config.WEEKDAYS; day++) {
                message += ("D" + day).PadRight(workerCountPadding+2);
            }
            message = message.TrimEnd()+"\nLP  ";

            for(int day=0; day<Config.WEEKDAYS; day++) {
                message += NEEDED_SHIFTS[day].ToString().PadRight(workerCountPadding+2);
            }
            message = message.TrimEnd()+"\n\nSchedule\n"+new string(' ', workerCountPadding+2);

            for(int day=1; day <= Config.WEEKDAYS; day++) {
                message += ("D" + day).PadRight(4);
            }
            message = message.TrimEnd() + "\n";

            foreach(Worker worker in WorkersTable) {
                message += worker.ShiftsToString() + "\n";
            }

            return message;
        }

        public void ForceFeasibility() {
            for (int day = 0; day < Config.WEEKDAYS; day++) {
                if (CurrentShifts[day] >= NEEDED_SHIFTS[day]) continue;
                List<Worker> mutationCandidates = WorkersTable.Where(worker => worker.AssignedShifts[day] == false).ToList();

                while (NEEDED_SHIFTS[day] > CurrentShifts[day]) {
                    if (mutationCandidates.Count == 0) throw new Exception("Not enough workers to cover day " + day);
                    Worker candidate = mutationCandidates[Program.Rand.Next(mutationCandidates.Count)];
                    bool success = candidate.AttemptMutation(day);

                    if (success) {
                        CurrentShifts[day]++;
                        mutationCandidates.Remove(candidate);
                    }
                }
            }
        }

        public bool CheckFeasibility() {
            bool feasible = true;
            for(int day=0; day<Config.WEEKDAYS; day++) {
                if (NEEDED_SHIFTS[day] > CurrentShifts[day]) {
                    feasible = false;
                    break;
                }
            }
            return feasible;
        }

        void RandomMutations() {
            int mutationCount = (int)Math.Floor(Program.currentGeneration/ CFG.GENERATION_CAP * sCFG.RANDOM_MUTATION_RATIO * WorkersTable.Length * Config.WEEKDAYS);
            for (int i = 0; i < mutationCount; i++) {
                int worker = Program.Rand.Next(WorkersTable.Length); //można tu zmienić aby nie wybierać tych samych kandydatów mutacji, ale zmniejszy to obszar przeszukiwany
                int day = Program.Rand.Next(7);
                WorkersTable[worker].AttemptMutation(day);
            }
        }


        void PointByPointCrossover(Schedule[] parents, List<int> pointByPointTargets) {
            foreach (int workerIndex in pointByPointTargets) {
                bool[] newSchedule = new bool[Config.WEEKDAYS];


                for (int day = 0; day < Config.WEEKDAYS; day++) {
                    int winner = RouletteWinner(FindWorkerWeights(parents, workerIndex));
                    newSchedule[day] = parents[winner].WorkersTable[workerIndex].AssignedShifts[day];
                }
                this.WorkersTable[workerIndex] = new(workerIndex, newSchedule);
            }
        }

        void CloneCrossover(Schedule[] parents, List<int> pointByPointTargets) {
            for (int workerIndex = 0; workerIndex < WorkersTable.Length; workerIndex++) {
                if (pointByPointTargets.Contains(workerIndex)) continue;
                int winner = RouletteWinner(FindWorkerWeights(parents, workerIndex));
                bool[] copiedWorkAssignments = (bool[])parents[winner].WorkersTable[workerIndex].AssignedShifts.Clone();
                this.WorkersTable[workerIndex] = new(workerIndex, copiedWorkAssignments);
            }
        }


        int RouletteWinner(double[] weights) {
            double randomWeight = Program.Rand.NextDouble();
            int index = Array.FindIndex(weights, weight => weight > randomWeight);
            return index == -1 ? 0 : index;
        }


        List<int> RandomizeCrossoverTargets() {
            List<int> pointByPointWorkers = Enumerable.Range(0, WorkersTable.Length).ToList();

            for (int i = 0; i < pointByPointWorkers.Count * (1 - sCFG.POINT_BY_POINT_RATIO); i++) {
                pointByPointWorkers.RemoveAt(Program.Rand.Next(pointByPointWorkers.Count));
            }
            return pointByPointWorkers;
        }


        double[] FindWorkerWeights(Schedule[] parents, int workerIndex) {
            double totalFitness = parents.Sum(parent => parent.WorkersTable[workerIndex].fitness);
            double[] weights = new double[parents.Length];
            weights[0] = parents[0].WorkersTable[workerIndex].fitness / totalFitness;
            for(int i=1; i<parents.Length; i++) {
                weights[i] = weights[i - 1] + parents[i].WorkersTable[workerIndex].fitness / totalFitness;
            }
            return weights;
        }


        double CalculateScheduleFitness() {
            double fitness = 0;
            foreach(Worker worker in WorkersTable) {
                worker.RecalculateFitness();
                fitness += worker.fitness;
            }
            return fitness;
        }


        public void RandomizeWorkers() {
            for (int i = 0; i < WorkersTable.Length; i++) {
                WorkersTable[i] = new Worker(i);
            }
            CalculateScheduleFitness();
            RecountShifts();
            ForceFeasibility();
        }
        
        void RecountShifts() {
            for(int day = 0; day<Config.WEEKDAYS; day++) {
                CurrentShifts[day] = this.WorkersTable.
                                     Where(worker => worker.AssignedShifts[day]).
                                     Count();
            }
        }

        public override string ToString() {
            return $"Fitness: {CalculateScheduleFitness()}";
        }
    }
}
