using CFG = Genetic_Algorithm.Config.ScheduleConfig;
using pCFG = Genetic_Algorithm.Config;
namespace Genetic_Algorithm
{
    public class Schedule
    {
        static readonly int[] NEEDED_SHIFTS;

        static Schedule() {
            NEEDED_SHIFTS = new int[pCFG.WEEKDAYS];
            for (int day=0; day< pCFG.WEEKDAYS; day++) {
                NEEDED_SHIFTS[day] = (int)Math.Ceiling(pCFG.WORKER_COUNT * CFG.SHIFT_PROPORTIONS[day]);
            }

        }


        Worker[] WorkersTable { get; set; }
        int[] CurrentShifts { get; set; }

        public Schedule(Schedule[] parents) : this() {
            List<int> pointByPointTargets = RandomizeCrossoverTargets();

            CloneCrossover(parents, pointByPointTargets);
            PointByPointCrossover(parents, pointByPointTargets);
            //TODO: modyfikacja metod mutacyjnych, aby usunąć szansę nieskończoności prób
            //być może zmiana obliczeń fitnessu wedle feasibility
            RandomMutations();
            if(Program.currentGeneration % pCFG.GENERATION_COUNT/10 == 0
                || Program.currentGeneration > pCFG.GENERATION_COUNT*0.9) FeasibilityMutations();
        }

        public Schedule() {
            WorkersTable = new Worker[pCFG.WORKER_COUNT];
            CurrentShifts = new int[pCFG.WEEKDAYS];
        }


        void FeasibilityMutations() {
            for (int day = 0; day < pCFG.WEEKDAYS; day++) {
                if (NEEDED_SHIFTS[day] == CurrentShifts[day]) continue;
               
                bool mutationWanted = NEEDED_SHIFTS[day] < CurrentShifts[day];
                Worker[] mutationCandidates = WorkersTable.Where(worker => worker.AssignedShifts[day] == !mutationWanted).ToArray();
                
                if (mutationCandidates.Length == 0) continue;

                while (NEEDED_SHIFTS[day] != CurrentShifts[day]) {
                    int worker = Program.Rand.Next(mutationCandidates.Length);
                    if (mutationCandidates[worker].AttemptMutation(day)) {
                        NEEDED_SHIFTS[day] += mutationWanted == true? 1 : -1;
                    }
                }
            }
        }

        void RandomMutations() {
            int mutationCount = (int)Math.Floor(Program.currentGeneration/ pCFG.GENERATION_COUNT * CFG.RANDOM_MUTATION_RATIO * WorkersTable.Length * pCFG.WEEKDAYS);
            for (int i = 0; i < mutationCount; i++) {
                int worker = Program.Rand.Next(WorkersTable.Length); //można tu zmienić aby nie wybierać tych samych kandydatów mutacji, ale zmniejszy to obszar przeszukiwany
                int day = Program.Rand.Next(7);
                WorkersTable[worker].AttemptMutation(day);
            }
        }


        void PointByPointCrossover(Schedule[] parents, List<int> pointByPointTargets) {
            foreach (int workerIndex in pointByPointTargets) {
                bool[] newSchedule = new bool[pCFG.WEEKDAYS];
                for (int day = 0; day < pCFG.WEEKDAYS; day++) {
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

            for (int i = 0; i < pointByPointWorkers.Count * (1 - CFG.POINT_BY_POINT_RATIO); i++) {
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


        public double CalculateScheduleFitness() {
            return WorkersTable.Sum(worker => worker.fitness);
        }


        public void RandomizeWorkers() {
            for (int i = 0; i < WorkersTable.Length; i++) {
                WorkersTable[i] = new Worker(i);
            }
            CalculateScheduleFitness();
            FeasibilityMutations();
            RecountShifts();
        }
        
        void RecountShifts() {
            for(int day = 0; day<pCFG.WEEKDAYS; day++) {
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
