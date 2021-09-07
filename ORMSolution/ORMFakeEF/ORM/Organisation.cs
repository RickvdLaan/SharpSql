using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORMFakeEF
{
    public class Organisation
    {
        [Key, ForeignKey("User")]
        public int Id { get; set; }
        public string Name { get; set; }
        public User User { get; set; }
    }
}
