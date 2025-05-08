using Genetic_Algorithm;
using CFG = Genetic_Algorithm.Config.ScheduleConfig;
using pCFG = Genetic_Algorithm.Config;

namespace Genetic_Algorithm {

    public static class Config {
        
        public const int WEEKDAYS = 7;
        public static int WORKER_COUNT = 50;
        public static int GENERATION_COUNT = 250;

        public static class ProgramConfig {
            public static bool PRINT_GENERATION_STATISTICS = true;
            public static int SCHEDULE_COUNT = 50;
            public static int PARENT_COUNT = 3;
            public static double ELITISM_RATIO = 0.05;
            public static double FORCE_FEASIBILITY_FREQUENCY = 0.1; //every 10% generations
            public static double FORCE_FEASIBILITY_FINAL = 0.2; //force feasibility in the last 20% too
        }

        public static class ScheduleConfig {
            public static double RANDOM_MUTATION_RATIO = 0.5;
            public static double POINT_BY_POINT_RATIO = 0.5;
            public static double[] SHIFT_PROPORTIONS = [0.8, 0.8, 0.8, 0.8, 0.8, 0.6, 0.4];
        }

        public static class WorkerConfig {
            public static double MAX_FITNESS = 8;
            public static double OFFDAY_WEIGHT = 0.5;
            public static double OVERWORK_WEIGHT = 0.3;
            public static double DISLIKED_DAY_WEIGHT = 0.2;
            public static double MUTATION_CHANCE = 0.2;

            //preferences
            public static int MAX_WORKDAYS = 5;
            public static int MAX_DISLIKED_DAYS = 3;
            public static double DISLIKED_CHANCE = 0.65;
            public static double OFFDAY_CHANCE = 0.2;
        }
    }
}