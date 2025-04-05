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
            public static double[] SHIFT_PROPORTIONS = [0.6, 1, 1, 1, 1, 0.6, 0.4];
        }

        public static class WorkerConfig {
            public static double MAX_FITNESS = 8;
            public static double OFFDAY_WEIGHT = 0.5;
            public static double OVERWORK_WEIGHT = 0.3;
            public static double DISLIKED_DAY_WEIGHT = 0.2;
            public static double MUTATION_CHANCE = 0.2;

            //preferences
            public const int MAX_WORKDAYS = 5;
            public static int MAX_DISLIKED_DAYS = 3;
            public static double DISLIKED_CHANCE = 0.65;
            public static double OFFDAY_CHANCE = 0.2;
        }
    }
}