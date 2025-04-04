using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSI_lab3
{
    public class BusinessSchedule
    {
        const int WORKER_COUNT = 4;
        public const int WEEKDAYS = 7;
        public const double SCHEDULE_WEIGHT = 0.5;
        public const double WORKER_WEIGHT = 0.5;

        public static readonly double[] SHIFT_PROPORTIONS = { 0.4, 0.6, 0.6, 0.6, 0.8, 0.4, 0.4 }; //losowo wygenerowane
        public static Worker[] Workers { get; } //lista pracowników, niezmienna
        public static double MaxFitness { get; } //fitness maksymalny, 50/50 fitness wszystkich pracowników i samego harmonogramu
        public static int[] NeededShifts { get; } //obliczone na podstawie ilości pracowników

        public WorkerSchedule[] ProposedWorkdays { get; set; }//genotyp


        //generuje pracownikow, oblicza max fitness
        static BusinessSchedule() {
            Workers = new Worker[4];
            for(int i=0; i<Workers.Length; i++) {
                Workers[i] = new();
            }
            MaxFitness = Workers.Length * Worker.MAX_FITNESS * 2;
            NeededShifts = new int[WEEKDAYS];
            for (int day = 0; day < WEEKDAYS; day++) {
                NeededShifts[day] = (int)Math.Ceiling(Workers.Length * SHIFT_PROPORTIONS[day]);
            }
        }

        //inicjalizuje plany na podstawie ilosci pracownikow
        public BusinessSchedule() {
            ProposedWorkdays = new WorkerSchedule[Workers.Length];
            for(int i=0; i<Workers.Length; i++) {
                ProposedWorkdays[i] = new WorkerSchedule(Workers[i]);
            }
            CoveredShifts = new int[Workers.Length];
        }


        int[] CoveredShifts { get; set; } //do obliczania wag
        public static BusinessSchedule Crossover(BusinessSchedule[] parents) {
            BusinessSchedule child = new BusinessSchedule();
            double[][] fitnesses = GetParentFitnessesByWorker(parents);
            Random rand = new Random();

            for (int worker = 0; worker < Workers.Length; worker++) {
                double[] dayWeights = WeightCalculator.DayWeights(child.CoveredShifts);
                double[] workerWeights = WeightCalculator.WorkerScheduleWeights(fitnesses[worker]);

                for(int day=0; day<WEEKDAYS; day++) {
                    //na pewno do optymalizacji
                    int parentToPick;
                    for (int parent = 0; parent < parents.Length; parent++) {
                        if (workerWeights[parent] > rand.NextDouble()) {
                            parentToPick = parent;
                            break;
                        }
                    } //nie no, musisz to jutro zrefaktoryzować cale bo fuj
                    //zlącz jakoś tak, żebyś nie musial tyle iterować po wszystkim
                    //dobrym pierwszym krokiem byloby odlożenie więcej do workerschedule, może polączenie z worker w jakiś sposób tak abyś mógl uzyskać dostęp po osobnikach businessschedule
                    //to można lepiej rozwiązać, tworząc poprawnie klasy
                    child.ProposedWorkdays[worker].AssignedWorkdays[day] =
                }
            }

            return child;
        }


        static double[][] GetParentFitnessesByWorker(BusinessSchedule[] parents) {
            double[][] fitnesses = new double[parents.Length][]; //po pracowniku, potem po businessschedule
            for (int i = 0; i < Workers.Length; i++) {
                for (int j = 0; j < parents.Length; j++) {
                    fitnesses[i][j] = parents[j].ProposedWorkdays[i].ScheduleFitness();
                }
            }
            return fitnesses;
        }


        public void RandomizeSchedule() {
            Random rand = new();
            foreach(WorkerSchedule workerSchedule in ProposedWorkdays) {
                bool[] workdays = new bool[WEEKDAYS];
                for(int i=0; i<WEEKDAYS; i++) {
                    workdays[i] = rand.NextDouble() < 0.5;
                }
            }
        }


        private static class WeightCalculator {

            public const double SCHEDULE_SIDE_WEIGHT = 0.5;
            public const double WORKER_SIDE_WEIGHT = 0.5;

            static double DayWeightLinearity = 0.75; //reszta wagi zostaje usunięta dopiero po wypelnieniu dnia
            //metoda daje dodatnią wagę jeżeli nie starczy pracowników danego dnia, ujemną jeżeli za dużo, czyli ujemna zwiększa wagę 0
            public static double[] DayWeights(int[] shiftCounts) {
                double[] weights = new double[WEEKDAYS];
                for (int i = 0; i < WEEKDAYS; i++) {
                    weights[i] = 1 - shiftCounts[i] / NeededShifts[i]; //jeżeli trzeba poprawki, możesz także skalować przez SHIFT_PROPORTIONS i przez wagi innych dni, ale to komplikuje sytuację
                    
                    if (shiftCounts[i] < NeededShifts[i]) {
                        weights[i] += 1 - DayWeightLinearity;
                    } else if (shiftCounts[i] > NeededShifts[i]) {
                        weights[i] -= 1 - DayWeightLinearity;
                    }

                    if (weights[i] > 1) { weights[i] = 1; }
                    else if (weights[i] < -1) { weights[i] = -1; }
                }
                return weights;
            }

            //być może trzeba będzie odrobinę mniej skalować, by nie doszlo do lokalnego ekstremum za szybko
            //szanse skumulowane dla foreach rand
            public static double[] WorkerScheduleWeights(double[] workerFitnesses) {
                double[] weights = new double[workerFitnesses.Length];
                double fitnessTotal = 0;
                foreach(double workerFitness in workerFitnesses) {
                    fitnessTotal += workerFitness;
                }

                weights[0] = workerFitnesses[0] / fitnessTotal;
                for (int i=1; i<weights.Length; i++) {
                    weights[i] = weights[i - 1] + workerFitnesses[i] / fitnessTotal;
                }
                return weights;
            }



        }

        private static class UnitTester {
        }

    }
}
