using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Data.Entities
{
    public abstract class CommandEntity
    {
        public int Id { get; set; }

        public int PlayerIndex { get; set; }

        public ActionState Result { get; set; }
    }
}
