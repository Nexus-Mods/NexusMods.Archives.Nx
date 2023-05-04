using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using JetBrains.Annotations;

namespace NexusMods.Archives.Nx.Tests.Attributes;

[PublicAPI]
public class AutoNonRandomDataAttribute : AutoDataAttribute
{
    public AutoNonRandomDataAttribute() : base(CreateFixture) { }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new DefaultConstructorSpecimenBuilder());
        return fixture;
    }
    
    public class DefaultConstructorSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is Type type && HasDefaultConstructor(type))
                return Activator.CreateInstance(type)!;

            return new NoSpecimen();
        }

        private static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
