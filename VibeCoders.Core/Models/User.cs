using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class User
    {
        public int id { get; set; }
        public string username { get; set; }
        public string passwordHash { get; set; }
    }
}
