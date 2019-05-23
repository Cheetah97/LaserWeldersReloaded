namespace EemRdx.Extensions
{
	public static class NumberExtensions
	{
        //public static int Squared(this int num)
        //{
        //	return (int)Math.Pow(num, 2);
        //}

        //public static int Cubed(this int num)
        //{
        //	return (int)Math.Pow(num, 3);
        //}

        //public static float Squared(this float num)
        //{
        //	return (float)Math.Pow(num, 2);
        //}

        //public static float Cubed(this float num)
        //{
        //	return (float)Math.Pow(num, 3);
        //}

        //public static float Root(this float num)
        //{
        //	return (float)Math.Sqrt(num);
        //}

        //public static float Cuberoot(this float num) => (float)Math.Pow(num, (double)1/3);

        public static double Squared(this double num)
        {
            return num * num;
        }

        //public static double Cubed(this double num)
        //{
        //	return Math.Pow(num, 3);
        //}

        //public static double Root(this double num)
        //{
        //	return Math.Sqrt(num);
        //}

        //public static double Cuberoot(this double num)
        //{
        //	return Math.Pow(num, (double)1/3);
        //}
    }
}