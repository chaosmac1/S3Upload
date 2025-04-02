using Microsoft.Extensions.DependencyInjection;

namespace Up3.DefaultNuget;

public interface IServiceBinder {
    public void Bind(IServiceCollection serviceCollection);
}