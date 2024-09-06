namespace twitter_clone.Services
{
    public static class ConfigurationSingleton
    {
        private static IConfiguration? _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    // Aquí inicializas la configuración, por ejemplo:
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");
                    _configuration = builder.Build();
                }

                return _configuration;
            }
        }

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }

}