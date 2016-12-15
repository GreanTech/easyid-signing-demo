namespace iframe_demo.Models
{
    public class SignatureModel
    {
        public string Ppid { get; set; }
        public string SignText { get; set; }
        public string Evidence { get; set; }
        public string Issuer { get; set; }

        public string EndorsingKeys { get; set; }
    }
}