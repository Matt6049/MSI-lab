using Genetic_Algorithm;
using CFG = Genetic_Algorithm.Configs.ScheduleConfig;
using pCFG = Genetic_Algorithm.Configs;

namespace Genetic_Algorithm {

    public static class Configs {
        
        public const int WEEKDAYS = 7;
        public static int WORKER_COUNT = 50;

        public static class ScheduleConfig {
            public static double RANDOM_MUTATION_RATIO = 0.25;
            public static double POINT_BY_POINT_RATIO = 0.5;
            public static readonly double[] SHIFT_PROPORTIONS = [0.6, 1, 1, 1, 1, 0.6, 0.4];
        }
    }
}