using System;
using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class TenantReg
    {
        [Key]
        public int TenantRegId { get; set; }

        [Required]
        public string UserName { get; set; }
        [Required]
        public string OriginalFunction { get; set; }
        [Required]
        public string Endpoint { get; set; }

    }
}