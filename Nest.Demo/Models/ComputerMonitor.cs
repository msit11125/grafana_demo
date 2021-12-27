using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nest.Demo.Models
{
    [InfluxDB.Client.Core.Measurement("computer_monitor")]
    public class ComputerMonitorInfux
    {
        [InfluxDB.Client.Core.Column("logId")]
        public string LogId { get; set; }

        [InfluxDB.Client.Core.Column("cpuUsage")]
        public float CpuUsage { get; set; }

        [InfluxDB.Client.Core.Column("memory")]
        public float Memory { get; set; }

        [InfluxDB.Client.Core.Column("time", IsTimestamp = true)]
        public DateTime Time { get; set; }

    }

    public class ComputerMonitor
    {
        public string LogId { get; set; }
        public float CpuUsage { get; set; }
        public float Memory { get; set; }
        public DateTime Time { get; set; }
        public virtual SystemInfo SystemInfo { get; set; }

        public virtual ICollection<Plugin> Plugins { get; set; }

        public ComputerMonitor()
        {
            Plugins = new HashSet<Plugin>();
        }
    }

    public class SystemInfo
    {
        public string UUID { get; set; }
        public string System { get; set; }
        public string Version { get; set; }
    }

    public class Plugin
    {
        public string Type { get; set; }
        public int CommPort { get; set; }
    }
}