using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.DBModels
{
    public static class DbInitializer
    {
        public static void Initialize(AutoQuestraderContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }
}
