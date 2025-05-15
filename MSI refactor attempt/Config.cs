using Genetic_Algorithm;
using Newtonsoft.Json;
using System.Linq;
using CFG = Genetic_Algorithm.Config.ScheduleConfig;
using pCFG = Genetic_Algorithm.Config;


namespace Genetic_Algorithm {

    public class Config {
        private static int ConfigIndexToUse = -1; //-1 for fresh instance using the below configs, 0 for newest
        public static Config ConfigSingleton { get; } = Config.InitializeConfig();

        public ProgramConfig pCFG { get; } = new ProgramConfig();
        public ScheduleConfig sCFG { get; } = new ScheduleConfig();
        public WorkerConfig wCFG { get; } = new WorkerConfig();


        public const int WEEKDAYS = 7;
        public int WORKER_COUNT { get; private set; } = 50;
        public int GENERATION_CAP { get; private set; } = 1000;

        public class ProgramConfig {
            public bool PRINT_GENERATION_STATISTICS { get; private set; } = true;
            public int SCHEDULE_COUNT { get; private set; } = 50;
            public int PARENT_COUNT { get; private set; } = 3;
            public double ELITISM_RATIO { get; private set; } = 0.05;

            //every n generations, force feasibility for m generations
            public double FORCE_FEASIBILITY_FREQUENCY { get; private set; } = 100;
            public int FORCE_FEASIBILITY_LENGTH { get; private set; } = 25;
            //if no change for n generations, force feasibility and end after converging again
            public int CONVERGENCE_COUNTDOWN_DURATION { get; private set; } = 50;
        }

        public class ScheduleConfig {
            public double RANDOM_MUTATION_RATIO { get; private set; } = 0.5;
            public double POINT_BY_POINT_RATIO { get; private set; } = 0.5;
            public double[] SHIFT_PROPORTIONS { get; private set; } = [0.8, 0.8, 0.8, 0.8, 0.8, 0.6, 0.4];
        }

        public class WorkerConfig {
            public double MAX_FITNESS { get; private set; } = 8;

            public double BASE_WEIGHT { get; private set; } = 0.5; //between 0 and 1
            //the three weights below must come out to a sum of 1
            public double OFFDAY_WEIGHT { get; private set; } = 0.5;
            public double OVERWORK_WEIGHT { get; private set; } = 0.3;
            public double DISLIKED_DAY_WEIGHT { get; private set; } = 0.2;

            public double MUTATION_CHANCE { get; private set; } = 0.2;

            //preferences
            public int MAX_WORKDAYS { get; private set; } = 5;
            public int MAX_DISLIKED_DAYS { get; private set; } = 3;
            public double DISLIKED_CHANCE { get; private set; } = 0.65;
            public double OFFDAY_CHANCE { get; private set; } = 0.2;
        }

        static Config InitializeConfig() {
            List<string> configList;
            Config outputConfig;
            if (!File.Exists("Config.json")) {
                configList = new();
            }
            else {
                string listJSON = File.ReadAllText("Config.json");
                configList = JsonConvert.DeserializeObject<List<string>>(listJSON) ?? new();
            }
            if (ConfigIndexToUse == -1 || configList == null) {
                outputConfig = new();
            }
            else {
                outputConfig = JsonConvert.DeserializeObject<Config>(configList[ConfigIndexToUse]) ?? new();
            }
            string configJSON = JsonConvert.SerializeObject(outputConfig);
            configList.Insert(0, configJSON);
            string listStringified = JsonConvert.SerializeObject(configList);
            File.WriteAllText("Config.json", listStringified);
            return outputConfig;
        }

        private Config() {}
    }
}