using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Collections;
public partial class Reader
{
    public Reader()
    {
        Console.WriteLine("Reader object has been created");
    }
    double filever;
    int paramcount, dofcount, loadvec_dim, timestepcount, offset;
    double interfaceh, Waterdepth, Hs, Hmax, Tp, WaveDir, dT, TotTim, smax;
    char[] ROSAVerNr, ROSAdateTimeID, ROSACaseName;
    char[] JESICAProgName, JESICAVerNr, JESICADateTimeID;
    List<double>[] mudline = new List<double>[8];
    OrderedDictionary parameters = new OrderedDictionary();
    public void Reading(string filename)
    {


        if (File.Exists(filename))
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))// Not sure of encoding requirement
            {
                /// INITIAL INFO
                var len = new FileInfo(filename).Length;

                Console.WriteLine("Size of file : {0}", len);
                filever = reader.ReadDouble();
                paramcount = reader.ReadInt32();
                Console.WriteLine(string.Format("Paramount : {0}", paramcount.ToString()));
                dofcount = reader.ReadInt32();
                Console.WriteLine(string.Format("Dofcount : {0}", dofcount.ToString()));
                loadvec_dim = reader.ReadInt32();
                Console.WriteLine(string.Format("loadvec_dim : {0}", loadvec_dim.ToString()));
                timestepcount = reader.ReadInt32();
                offset = reader.ReadInt32();
                Console.WriteLine(string.Format("Offset : {0}", offset.ToString()));

                /// SIMULATION INFORMATIONS
                
                interfaceh = reader.ReadDouble();
                ROSAVerNr = reader.ReadChars(8);
                ROSAdateTimeID = reader.ReadChars(20);
                ROSACaseName = reader.ReadChars(60);
                Waterdepth = reader.ReadDouble();
                Hs = reader.ReadDouble();
                Hmax = reader.ReadDouble();
                Tp = reader.ReadDouble();
                WaveDir = reader.ReadDouble();
                dT = reader.ReadDouble();
                TotTim = reader.ReadDouble();

                /// MUDLINE LOAD SUMMARY
                
                for (int i = 0; i <= 7; i++)
                {
                    mudline[i] = new List<double>();
                    for (int j = 0; j < 4; j++)
                    {
                        mudline[i].Add(reader.ReadDouble());
                    }
                }
                for (int i = 0; i <= 7; i++)
                {
                    mudline[i].ForEach(j => Console.Write("\n{0}", j));
                    Console.Write("\n");
                }
                
                ///PROGRAM INFO 8ADDITIONAL INFORMATIONS)
                
                JESICAProgName = reader.ReadChars(8);
                Console.WriteLine(JESICAProgName);
                JESICAVerNr = reader.ReadChars(8);
                Console.WriteLine(JESICAVerNr);
                JESICADateTimeID = reader.ReadChars(20);
                smax = reader.ReadDouble();

                /// SKIPPING TO DATA USING OFFSET
                
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                /// SHIELA PARAMETERS

                parameters.Add("keys", "values");
                ICollection keys = parameters.Keys;
                ICollection values = parameters.Values;
                for (int i = 0; i < paramcount; i++)
                {
                    keys = reader.ReadChars(12);
                    values = reader.ReadChars(12);
                    parameters.Add(keys, values);
                }

                int superstart = offset + (paramcount * 2 * 12);
                Console.Write(superstart);
                reader.Close();

                ///SUPER ELEMENT MATRICES
                
                double[,] mmat = new double[dofcount, dofcount];
                double[,] dmat = new double[dofcount, dofcount];
                double[,] smat = new double[dofcount, dofcount];
                Superelements super = new Superelements();
                mmat = super.supe(dofcount, filename, superstart);
                superstart += (1 + dofcount) * dofcount / 2 * 8;
                dmat = super.supe(dofcount, filename, superstart);
                superstart += (1 + dofcount) * dofcount / 2 * 8;
                smat = super.supe(dofcount, filename, superstart);
                superstart += (1 + dofcount) * dofcount / 2 * 8;
                super.superelement_print(mmat, dofcount);
                super.superelement_print(smat, dofcount);
                //reader.BaseStream.Seek(superstart, SeekOrigin.Begin);

                List<double>[] loadvec = new List<double>[timestepcount];
                for (int i = 0; i < timestepcount; i++)
                {
                    loadvec[i] = new List<double>();
                    for (int j = 0; j < loadvec_dim; j++)
                    {
                        for (int k = 0; k < 8 * loadvec_dim + 2; k++)
                        {
                            NewMethod(reader, loadvec, i);
                        }
                    }

                }

            }
        }
        else
        {
            Console.WriteLine("File not found or cannot be opened!!!");
        }
    }

    private static void NewMethod(BinaryReader reader, List<double>[] loadvec, int i)
    {
        loadvec[i].Add(reader.ReadDouble());
    }
}

