using pCFG = Genetic_Algorithm.Config;
using CFG = Genetic_Algorithm.Config.ProgramConfig;

namespace Genetic_Algorithm {
    internal class Program {
        public static int currentGeneration;
        public static Random Rand = new();

        static Schedule[] currentPopulation;
        static Schedule[] children;
        static int elitismCarryoverCount;



        static void Main(string[] args) {
            DateTime timeStart = DateTime.Now;
            currentPopulation = new Schedule[CFG.SCHEDULE_COUNT];
            elitismCarryoverCount = (int)Math.Ceiling(CFG.SCHEDULE_COUNT * CFG.ELITISM_RATIO);
            currentGeneration = 1;


            for (int i = 0; i < CFG.SCHEDULE_COUNT; i++) {
                currentPopulation[i] = new();
                currentPopulation[i].RandomizeWorkers();
            }


            while (currentGeneration <= pCFG.GENERATION_COUNT) {
                if (CFG.PRINT_GENERATION_STATISTICS || currentGeneration == pCFG.GENERATION_COUNT) {
                    PrintPopulation();
                }
                children = new Schedule[CFG.SCHEDULE_COUNT];

                Schedule[] bestParents = currentPopulation.OrderBy(sched => sched.CalculateScheduleFitness()).TakeLast(elitismCarryoverCount).ToArray();
                for (int i = 0; i < elitismCarryoverCount; i++) {
                    children[i] = bestParents[i];
                }

                //ustal elityzm tylko dla wlaściwych rozwiązań
                for (int i = elitismCarryoverCount; i < CFG.SCHEDULE_COUNT; i++) {
                    List<int> candidates = Enumerable.Range(0, CFG.SCHEDULE_COUNT).ToList();
                    Schedule[] parents = new Schedule[CFG.PARENT_COUNT];
                    for (int j = 0; j < CFG.PARENT_COUNT; j++) {
                        int candidate = candidates[Rand.Next(candidates.Count)];
                        candidates.Remove(candidate);
                        parents[j] = currentPopulation[candidate];
                    }
                    children[i] = new(parents);
                }
                currentPopulation = children;
                FeasibilityCheck();
                TryFixFeasibility();
                currentGeneration++;
            }
            DateTime timeEnd = DateTime.Now;
            PrintBestSchedule();
            Console.WriteLine($"Czas: {timeEnd - timeStart}");
            
        }

        static int FeasibilityCountdown = 0;
        private static void FeasibilityCheck() {
            if ((currentGeneration+1)%CFG.FORCE_FEASIBILITY_FREQUENCY == 0
            || currentGeneration > pCFG.GENERATION_COUNT - CFG.FORCE_FEASIBILITY_FINAL) {
                FeasibilityCountdown = CFG.FORCE_FEASIBILITY_LENGTH;
            }
        }


        private static void TryFixFeasibility() {
            if (FeasibilityCountdown <= 0) return;
            FeasibilityCountdown--;
            foreach (Schedule sched in currentPopulation) {
                sched.ForceFeasibility();
            }
        }


        static void PrintPopulation() {
            double max = currentPopulation.Max(Sched => Sched.CalculateScheduleFitness());
            double avg = currentPopulation.Average(Sched => Sched.CalculateScheduleFitness());

            Console.WriteLine($"Generacja: {currentGeneration}, Fitness maksymalny: {max}, Fitness średni: {avg}");
        }


        static void PrintBestSchedule() {
            Schedule best = currentPopulation.OrderByDescending(Sched => Sched.CalculateScheduleFitness()).First();
            best.PrintWorkerSchedules();
            best.PrintShifts();
        }
    }
}
