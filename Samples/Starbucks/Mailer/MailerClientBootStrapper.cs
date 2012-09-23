using Castle.MicroKernel.Registration;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas.Persisters;

namespace Starbucks.Mailer
{
    public class MailerClientBootStrapper : CastleBootStrapper
    {
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
        }

        protected override bool IsTypeAcceptableForThisBootStrapper(System.Type t)
        {
            return t.Namespace == "Starbucks.Mailer";
        }
    }
}
