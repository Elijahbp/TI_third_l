using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace third_l
{
    class Program
    {
        //ПРИ КОДИРОВКЕ UTF-8

        /* TODO
         * 1) транспонирование матрицы +
         * 2) образование таблицы синдромов +
         * 3) поправить проверку на ошибки (использовать таблицу синдромов) +
         * 4) в декодере настроить сделать определение ошибок и их моментальное исправление.
         * После чего сделать повторную проверку. + 
         *  
         */
        static void Main(string[] args)
        {
            /*
             *  {1,0,0,0,0,1,1,1},
                {0,1,0,0,1,0,1,1},
                {0,0,1,0,1,1,0,1},
                {0,0,0,1,1,1,1,0}
             */
            int[,] matrixG = new int[,]{
                {1,0,0,0,0,1,1},
                {0,1,0,0,1,1,1},
                {0,0,1,0,1,1,0},
                {0,0,0,1,1,0,1}
            };

            string inputstring = "ыф83";
            new Program(matrixG, inputstring);
        }

        string inputString;
        private int[,] matrixG;
        private int[,] matrixHt;
        private int[,] tableSyndromes;

        public Program(int[,] matrixG, string inputString)
        {
            this.inputString = inputString;

            this.matrixG = matrixG;

            this.matrixHt = TransposingMatrix(matrixG);

            this.tableSyndromes = GenerateTableSyndromes(matrixHt);


           
            byte[] inputStringInBytes = ASCIIEncoding.UTF8.GetBytes(inputString.ToCharArray());

            List<string> binaryList = new List<string>();
            List<string> sepList = new List<string>();
            List<int> resultList = new List<int>();
            foreach (byte c in inputString)
            {
               
                binaryList.Add(Convert.ToString(c, 2).PadLeft(8, '0'));
                
            };

            string leftPart ="";
            string rightPart = "";
            foreach(string s in binaryList)
            {
                Console.WriteLine(s);
                BinariSeparator(s, matrixG,out leftPart, out rightPart);
                sepList.Add(leftPart);
                sepList.Add(rightPart);
            }

            foreach(string s in sepList)
            {
                resultList.Add(Coder(s));
            }

            resultList.ForEach(delegate(int s)
            {
                CheckOnIntegrity(s);
            });
            Console.WriteLine("Проверка на ошибки прошла успешно!");

            Console.ReadKey();
        }

        private static int[,] TransposingMatrix(int[,] matrix)
        {
            int[,] transposedMatrix = new int [matrix.GetLength(1),matrix.GetLength(0)];

            int r = matrix.GetLength(0)-1;
            int c = matrix.GetLength(1)-1;

            for (int i = 0; i < transposedMatrix.GetLength(1) ; i++)
            {
                c = matrix.GetLength(1) - 1;
                for (int j = 0; j < transposedMatrix.GetLength(0); j++)
                {
                    int l = matrix[r, c];
                    transposedMatrix[j,i] = matrix[r,c];
                    c--;
                }
                if (r == 0)
                {
                    break;
                }
                r--;
            }

            return transposedMatrix;
        }

        private static int[,] GenerateTableSyndromes(int[,] matrixHt)
        {
            int[,] tableSyndromes = new int[2,matrixHt.GetLength(0)];
            int numberOfDigits = tableSyndromes.GetLength(1);

            //Добавление числовых значений как индексов
            StringBuilder stringBuilder = new StringBuilder();
            for (int i =0; i< tableSyndromes.GetLength(1);i++)
            {
                for (int j = 0; j < matrixHt.GetLength(1); j++)
                {
                    stringBuilder.Append(matrixHt[i,j]);
                }
                tableSyndromes[0,i] = Convert.ToInt16(stringBuilder.ToString(),2);
                stringBuilder.Clear();
            }

            //Добавление разрядов для исправлений
            for (int i = 0; i < tableSyndromes.GetLength(1); i++)
            {
                stringBuilder.Append("1");
                for (int j = 1; j < numberOfDigits; j++)
                {
                    stringBuilder.Append("0");
                }

                tableSyndromes[1, i] = Convert.ToInt16(stringBuilder.ToString(), 2);
                stringBuilder.Clear();
                numberOfDigits--;
            }

            return tableSyndromes;
        }

        private static bool BinariSeparator(String symbol, int[,] matrixG,out string leftPart,out string rightPart)
        {

            if(symbol.Length != 8)
            {
                throw new Exception("Длина битового представления символа > 8");
            }

            leftPart = symbol.Substring(0,4);
            rightPart = symbol.Substring(4, 4);
            return true;
           
            

        }

        private int Coder(string part)
        {
            char[] bits = part.ToCharArray();

            StringBuilder totalStr = new StringBuilder();

            int r = 0;
            for (int i = 0; i < matrixG.GetLength(1); i++)
            {
                r = 0;
                for (int j = 0; j < matrixG.GetLength(0); j++)
                {
                    r ^=  int.Parse(bits[j].ToString()) & matrixG[j, i];
                }
                totalStr.Append(r);
            }
            Console.WriteLine(totalStr.ToString());
            return Convert.ToInt16(totalStr.ToString(),2);
        }

        private bool CheckOnIntegrity(int codeWordInt)
        {

            int valueCodeWord = codeWordInt;
            char[] bitsCodeWord = Convert.ToString(valueCodeWord,2).PadLeft(8,'0').ToCharArray();
            int syndrome =0; 
            int r;
            StringBuilder totalStr = new StringBuilder();
            for (int i = 0; i < matrixHt.GetLength(1); i++)
            {
                r = 0;
                for (int j = 0; j < matrixHt.GetLength(0); j++)
                {
                    r ^= int.Parse(bitsCodeWord[j].ToString()) & matrixHt[j, i];
                }
                totalStr.Append(r);
            }

            syndrome = Convert.ToInt16(totalStr.ToString(),2);
            if (syndrome != 0)
            {
                for(int i =0; i < tableSyndromes.GetLength(0); i++)
                {
                    if (tableSyndromes[i,0] == syndrome)
                    {
                        valueCodeWord = RepairValue(valueCodeWord,tableSyndromes[i,1]);
                        if (!CheckOnIntegrity(valueCodeWord))
                        {
                            throw new Exception("АЛЯРМ");
                        }
                    }
                    else
                    {
                        //придумать форму записи, в случае отсутствия синдрома
                    }
                    
                }
                
            }
            return true; 
        }

        private static int RepairValue(int word, int valueFromTableSyndrome)
        {
            return word ^ valueFromTableSyndrome; 
        }


    }
}
/*/6-й вариант
byte[,] matrixG =
{
    {1,0,0,0,0,1,1},
    {0,1,0,0,1,1,1},
    {0,0,1,0,1,1,0},
    {0,0,0,1,1,0,1}
};

byte[,] matrixHt =
{
    {1,0,1,1},
    {0,1,1,1},
    {1,1,1,0},
    {1,0,0,0},
    {0,1,0,0},
    {0,0,1,0},
    {0,0,0,1}
};
*/
//{
//    {0,1,1,1},
//    {1,0,1,1},
//    {1,1,0,1},
//    {1,1,1,0},
//    {1,0,0,0},
//    {0,1,0,0},
//    {0,0,1,0},
//    {0,0,0,1}
//};