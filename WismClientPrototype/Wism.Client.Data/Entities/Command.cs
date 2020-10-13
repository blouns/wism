using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Wism.Client.Data.Entities
{
    public class Command
    {
        [Key]
        public int Id { get; set; }
    }
}