using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSI_lab3
{
    public class Worker
    {
        public const int MAX_DISLIKED_DAYS = 3;
        public const int MAX_WORKDAYS = 5;
        public const double MAX_FITNESS = 16;
        public bool[] DislikedWorkdays { get; init; }
        public bool[] OffDays { get; init; }
        public int dislikedCount = 0;
        public int offDaysCount = 0;
        
        public Worker() {
            DislikedWorkdays = new bool[BusinessSchedule.WEEKDAYS];
            OffDays = new bool[BusinessSchedule.WEEKDAYS];

            Random rand = new Random();
            DislikedWorkdays[6] = rand.NextDouble() < 0.7;
            for(int i=1; i<MAX_DISLIKED_DAYS; i++) {
                if (rand.NextDouble() < 0.5) {
                    DislikedWorkdays[rand.Next(6)] = true;
                }
            }
            for(int i=0; i<OffDays.Length; i++) {
                double chance = DislikedWorkdays[i] ? 0.2 : 0.1;
                if(rand.NextDouble() < chance) {
                    OffDays[i] = true;
                }
            }

            dislikedCount = DislikedWorkdays.Where(workday => workday).Count();
            offDaysCount = OffDays.Where(offday => offday).Count();
        }

        
    }
}
