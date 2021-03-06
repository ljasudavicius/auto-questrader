﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Design;

namespace BLL.DBModels
{
    public partial class AutoQuestraderContext : DbContext
    {
        public AutoQuestraderContext(DbContextOptions<AutoQuestraderContext> options) : base(options)
        {
        }

        public virtual DbSet<AccountCategory> AccountCategories { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<SettingValues> SettingValues { get; set; }
        public virtual DbSet<StockTarget> StockTargets { get; set; }
        public virtual DbSet<Token> Tokens { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Account> Accounts { get; set; } 
    }

    //public class AutoQuestraderContextFactory : IDesignTimeDbContextFactory<AutoQuestraderContext>
    //{
    //    public AutoQuestraderContext CreateDbContext(string[] args)
    //    {
    //        var optionsBuilder = new DbContextOptionsBuilder<AutoQuestraderContext>();

    //        var connectionString = Configuration.GetConnectionString("AutoQuestraderDatabase");

    //        optionsBuilder.UseSqlServer(connectionString);
     
    //        return new AutoQuestraderContext(optionsBuilder.Options);
    //    }
    //}
}
