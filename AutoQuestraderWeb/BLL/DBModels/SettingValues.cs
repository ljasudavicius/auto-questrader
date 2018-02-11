using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.DBModels
{
    public class SettingValues
    {
        [Key]
        [StringLength(50)]
        public string Name { get; set; }

        public string Value { get; set; }
    }
}
