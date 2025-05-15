namespace Genetic_Algorithm {
    internal class Program {
        static Config CFG = Config.ConfigSingleton;
        static Config.ProgramConfig pCFG = CFG.pCFG;


        public static int currentGeneration;
        public static Random Rand = new();
        enum CONVERGENCE_PROGRESS {
            NOT_CONVERGING,
            IS_CONVERGING,
            CONVERGED
        }

        static Schedule[] currentPopulation;
        static Schedule[] elites = [];


        static void Main(string[] args) {
            DateTime timeStart = DateTime.Now;
            currentPopulation = new Schedule[pCFG.SCHEDULE_COUNT];
            elitismCarryoverCount = (int)Math.Ceiling(pCFG.SCHEDULE_COUNT * pCFG.ELITISM_RATIO);
            currentGeneration = 1;


            for (int i = 0; i < pCFG.SCHEDULE_COUNT; i++) {
                currentPopulation[i] = new();
                currentPopulation[i].RandomizeWorkers();
            }

            CONVERGENCE_PROGRESS convergenceState = CONVERGENCE_PROGRESS.NOT_CONVERGING;
            while (currentGeneration <= CFG.GENERATION_CAP && convergenceState != CONVERGENCE_PROGRESS.CONVERGED) {
                if (pCFG.PRINT_GENERATION_STATISTICS) {
                    PrintPopulation();
                }
                elites = FindEliteSchedules();
                currentPopulation = CreateNextPopulation();
                if (ConvergenceCheck() == true) {
                    convergenceState++;
                }
                ProcessFeasibility(convergenceState);
                currentGeneration++;
            }
            DateTime timeEnd = DateTime.Now;
            PrintBestSchedule();
            PrintPopulation();
            Console.WriteLine($"Czas: {timeEnd - timeStart}");
            
        }

        private static Schedule[] CreateNextPopulation() {
            Schedule[] children = new Schedule[pCFG.SCHEDULE_COUNT];
            int childrenToCreate = currentPopulation.Length - elites.Length;

            for (int i = 0; i < elites.Length; i++) {
                children[i] = elites[i];
            }


            for (int i = elites.Length; i < pCFG.SCHEDULE_COUNT; i++) {
                List<int> candidates = Enumerable.Range(0, pCFG.SCHEDULE_COUNT).ToList();
                Schedule[] parents = new Schedule[pCFG.PARENT_COUNT];

                for (int j = 0; j < pCFG.PARENT_COUNT; j++) {
                    int candidate = candidates[Rand.Next(candidates.Count)];
                    candidates.Remove(candidate);
                    parents[j] = currentPopulation[candidate];
                }
                children[i] = new(parents);
            }
            return children;
        }

        static int ConvergenceCountdown = pCFG.CONVERGENCE_COUNTDOWN_DURATION;
        static double PreviousMax = 0;
        private static bool ConvergenceCheck() {
            if (elites.Length <= 0) return false;

            double maxFitness = elites[0].ScheduleFitness;
            if(maxFitness != PreviousMax) {
                ConvergenceCountdown = pCFG.CONVERGENCE_COUNTDOWN_DURATION;
                PreviousMax = maxFitness;
                return false;
            }
            ConvergenceCountdown--;
            if (ConvergenceCountdown <= 0) {
                ConvergenceCountdown = pCFG.CONVERGENCE_COUNTDOWN_DURATION;
                return true;
            }
            return false;
        }

        private static void ProcessFeasibility(CONVERGENCE_PROGRESS status) {
            if (status == CONVERGENCE_PROGRESS.NOT_CONVERGING) {
                FeasibilityCheck();
                TryFixFeasibility();
            }
            else if (status == CONVERGENCE_PROGRESS.IS_CONVERGING) {
                ForceFixFeasibility();
            }
        }

        static int FeasibilityCountdown = 0;
        private static void FeasibilityCheck() {
            if ((currentGeneration+1)%pCFG.FORCE_FEASIBILITY_FREQUENCY == 0) {
                FeasibilityCountdown = pCFG.FORCE_FEASIBILITY_LENGTH;
            }
        }


        private static void TryFixFeasibility() {
            if (FeasibilityCountdown <= 0) return;
            FeasibilityCountdown--;
            ForceFixFeasibility();
        }

        private static void ForceFixFeasibility() {
            foreach (Schedule sched in currentPopulation) {
                sched.ForceFeasibility();
            }
        }

        static int elitismCarryoverCount;
        private static Schedule[] FindEliteSchedules() {
            return currentPopulation.Where(sched => sched.CheckFeasibility())
                                    .OrderBy(sched => sched.ScheduleFitness)
                                    .TakeLast(elitismCarryoverCount)
                                    .ToArray();
        }


        static void PrintPopulation() {
            double max = currentPopulation.Max(Sched => Sched.ScheduleFitness);
            double avg = currentPopulation.Average(Sched => Sched.ScheduleFitness);

            Console.WriteLine($"Generacja: {currentGeneration}, Fitness maksymalny: {max}, Fitness średni: {avg}");
        }

        static void PrintBestSchedule() {
            Schedule best = currentPopulation.OrderByDescending(Sched => Sched.ScheduleFitness).First();
            best.PrintWorkerSchedules();
            best.PrintShifts();
        }
    }
}
