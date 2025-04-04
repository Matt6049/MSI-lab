using MSI_refactor_attempt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSI_refactor_attempt
{
    public class Schedule
    {
        public const int WORKER_COUNT = 50;
        public const int WEEKDAYS = 7;
        const int MUTATION_ATTEMPTS = 10;
        const double RANDOM_MUTATION_RATIO = 0.25;
        const double POINT_BY_POINT_RATIO = 0.5;
        static readonly double[] SHIFT_PROPORTIONS = [0.6, 1, 1, 1, 1, 0.6, 0.4];
        static readonly int[] NEEDED_SHIFTS;

        static Schedule() {
            NEEDED_SHIFTS = new int[WEEKDAYS];
            for (int day=0; day<WEEKDAYS; day++) {
                NEEDED_SHIFTS[day] = (int)Math.Ceiling(WORKER_COUNT * SHIFT_PROPORTIONS[day]);
            }

        }


        Worker[] WorkersTable { get; set; }
        Random Rand { get; set; }
        int[] CurrentShifts { get; set; }

        public Schedule(Schedule[] parents) : this() {
            List<int> pointByPointTargets = RandomizeCrossoverTargets();

            CloneCrossover(parents, pointByPointTargets);
            PointByPointCrossover(parents, pointByPointTargets);
            RandomMutations();
            FeasibilityMutations();
        }

        void FeasibilityMutations() {
            for (int day = 0; day < WEEKDAYS; day++) {
                if (NEEDED_SHIFTS[day] == CurrentShifts[day]) continue;
                bool mutationType = NEEDED_SHIFTS[day] > CurrentShifts[day];
                Worker[] mutationCandidates = WorkersTable.Where(worker => worker.AssignedWorkdays[day] == mutationType).ToArray();
                if (mutationCandidates.Length == 0) continue;

                int attempts = MUTATION_ATTEMPTS;
                while (NEEDED_SHIFTS[day] != CurrentShifts[day] && attempts > 0) {
                    int worker = Rand.Next(mutationCandidates.Length);
                    mutationCandidates[worker].AttemptMutation(day);
                    attempts--;
                }
            }
        }

        void RandomMutations() {
            int mutationCount = (int)Math.Floor(Program.currentGeneration/Program.GENERATIONS_COUNT * RANDOM_MUTATION_RATIO * WorkersTable.Length * WEEKDAYS);
            for (int i = 0; i < mutationCount; i++) {
                int worker = Rand.Next(WorkersTable.Length);
                int day = Rand.Next(7);
                WorkersTable[worker].AttemptMutation(day);
            }
        }

        public Schedule() {
            Rand = new();
            WorkersTable = new Worker[WORKER_COUNT];
            CurrentShifts = new int[WEEKDAYS];
        }


        void PointByPointCrossover(Schedule[] parents, List<int> pointByPointTargets) {
            foreach (int workerIndex in pointByPointTargets) {
                bool[] newSchedule = new bool[WEEKDAYS];
                for (int day = 0; day < WEEKDAYS; day++) {
                    int winner = TourneyWinner(FindWorkerWeights(parents, workerIndex));
                    newSchedule[day] = parents[winner].WorkersTable[workerIndex].AssignedWorkdays[day];
                }
                this.WorkersTable[workerIndex] = new(workerIndex, newSchedule);
            }
        }

        void CloneCrossover(Schedule[] parents, List<int> pointByPointTargets) {
            for (int workerIndex = 0; workerIndex < WorkersTable.Length; workerIndex++) {
                if (pointByPointTargets.Contains(workerIndex)) continue;
                int winner = TourneyWinner(FindWorkerWeights(parents, workerIndex));
                bool[] copiedWorkAssignments = (bool[])parents[winner].WorkersTable[workerIndex].AssignedWorkdays.Clone();
                this.WorkersTable[workerIndex] = new(workerIndex, copiedWorkAssignments);
            }
        }


        int TourneyWinner(double[] weights) {
            double randomWeight = Rand.NextDouble();
            int index = Array.FindIndex(weights, weight => weight > randomWeight);
            return index == -1 ? 0 : index;
        }


        List<int> RandomizeCrossoverTargets() {
            List<int> pointByPointWorkers = Enumerable.Range(0, WorkersTable.Length).ToList();

            for (int i = 0; i < pointByPointWorkers.Count * (1 - POINT_BY_POINT_RATIO); i++) {
                pointByPointWorkers.RemoveAt(Rand.Next(pointByPointWorkers.Count));
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
                WorkersTable[i] = new Worker(i, GetRandomShifts());
            }
            CalculateScheduleFitness();
            FeasibilityMutations();
        }
        

        bool[] GetRandomShifts() {
            bool[] shifts = new bool[WEEKDAYS];
            for(int day=0; day<WEEKDAYS; day++) {
                if (Rand.NextDouble() < (double)(Worker.MAX_WORKDAYS) / WEEKDAYS) {
                    shifts[day] = true;
                    CurrentShifts[day]++;
                }
            }
            return shifts;
        }

        public override string ToString() {
            return $"Fitness: {CalculateScheduleFitness()}";
        }
    }
}
