using DMFT.Services;

namespace DMFT
{
    public partial class App : Application
    {
        private readonly SeleniumServices seleniumServices;

        public App(SeleniumServices seleniumServices)
        {
            InitializeComponent();
            // Đăng ký sự kiện ProcessExit
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            this.seleniumServices = seleniumServices;
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            seleniumServices.Dispose();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "DMFT" };
        }
    }
}
