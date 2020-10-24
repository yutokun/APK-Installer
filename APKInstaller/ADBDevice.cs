namespace APKInstaller
{
    public class ADBDevice
    {
        public string Serial;
        public string Model;
        public string State;
        public bool IsUnauthorized => State == "unauthorized";
        public bool IsValidDevice => State == "device";
    }
}
