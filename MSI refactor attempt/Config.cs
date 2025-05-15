using Genetic_Algorithm;
using CFG = Genetic_Algorithm.Config.ScheduleConfig;
using pCFG = Genetic_Algorithm.Config;

namespace Genetic_Algorithm {

    public static class Config {
        
        public const int WEEKDAYS = 7;
        public static int WORKER_COUNT = 10;
        public static int GENERATION_CAP = 10000;

        public static class ProgramConfig {
            public static bool PRINT_GENERATION_STATISTICS = true;
            public static int SCHEDULE_COUNT = 50;
            public static int PARENT_COUNT = 3;
            public static double ELITISM_RATIO = 0.05;

            //every n generations, force feasibility for m generations
            public static double FORCE_FEASIBILITY_FREQUENCY = 100;
            public static int FORCE_FEASIBILITY_LENGTH = 25;
            //if no change for n generations, force feasibility and end after converging again
            public static int CONVERGENCE_COUNTDOWN_DURATION = 50;
        }

        public static class ScheduleConfig {
            public static double RANDOM_MUTATION_RATIO = 0.5;
            public static double POINT_BY_POINT_RATIO = 0.5;
            public static double[] SHIFT_PROPORTIONS = [0.8, 0.8, 0.8, 0.8, 0.8, 0.6, 0.4];
        }

        public static class WorkerConfig {
            public static double MAX_FITNESS = 8;

            public static double BASE_WEIGHT = 0.5; //between 0 and 1
            //the three weights below must come out to a sum of 1
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