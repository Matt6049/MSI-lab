using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSI_lab3
{
    public class WorkerSchedule
    {
        public Worker Assigned { get; set; }
        public bool[] AssignedWorkdays { get; set; }

        public WorkerSchedule(Worker assigned) {
            this.Assigned = assigned;
            AssignedWorkdays = new bool[BusinessSchedule.WEEKDAYS];
        }


        public double ScheduleFitness() {
            double fitness = Worker.MAX_FITNESS;
            int dislikedProposed = 0;
            int offDaysProposed = 0;
            int totalWorkdays = AssignedWorkdays.Where(workday => workday).Count();

            for (int i = 0; i < Assigned.DislikedWorkdays.Length; i++) {
                if (AssignedWorkdays[i] && Assigned.DislikedWorkdays[i]) {
                    dislikedProposed++;
                }
                if (AssignedWorkdays[i] && Assigned.OffDays[i]) {
                    offDaysProposed++;
                }
            }
            fitness -= (int)Math.Pow(Math.Pow(Worker.MAX_FITNESS * 0.5, 1 / Assigned.dislikedCount), dislikedProposed); //aby nie bylo kozla ofiarnego w algorytmie
            if (totalWorkdays > Worker.MAX_WORKDAYS) {
                fitness -= 8;
            }
            else {
                fitness -= 8 * offDaysProposed / Assigned.offDaysCount;
            }
            return fitness;
        }
    }
}
