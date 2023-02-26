
namespace CloudBackup.Common
{
    public static class NumberExtensions
    {
        public static int Modulus(this int number, int divisor)
        {
            var remainder = number % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        public static long Modulus(this long number, long divisor)
        {
            var remainder = number % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        public static long Modulus(this byte number, byte divisor)
        {
            var remainder = number % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }
    }
}