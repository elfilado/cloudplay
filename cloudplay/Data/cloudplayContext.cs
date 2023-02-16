using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using cloudplay.Models;

namespace cloudplay.Data
{
    public class cloudplayContext : DbContext
    {
        public cloudplayContext (DbContextOptions<cloudplayContext> options)
            : base(options)
        {
        }

        public DbSet<cloudplay.Models.Person> Person { get; set; } = default!;
    }
}
