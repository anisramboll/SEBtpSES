using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEB2SES
{
    public class Reader
    {
        ///Constructor
        int paramcount, loadvec_dim, timestepcount, offset, superstart, dofcount, i_n;
        double interfaceh, Waterdepth, Hs, Hmax, Tp, WaveDir, dT, TotTim, smax, filever;
        char[] ROSAVerNr, ROSAdateTimeID, ROSACaseName;
        char[] JESICAProgName, JESICAVerNr, JESICADateTimeID;
        List<double>[] mudline;//= new List<double>[8];
        List<double>[] loadvec;// = new List<double>[6];
        OrderedDictionary parameters = new OrderedDictionary();
        double[,] mmat, smat, dmat;
        public Reader Reading(string filename)
        {
            BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open));
            Reader r = new Reader();
            try 
            {
                using (reader)
                {
                    /// INITIAL INFO
                    r.filever = reader.ReadDouble();
                    r.i_n = Convert.ToInt32(r.filever);
                    r.paramcount = reader.ReadInt32();
                    r.dofcount = reader.ReadInt32();
                    r.loadvec_dim = reader.ReadInt32();
                    r.timestepcount = reader.ReadInt32();
                    r.offset = reader.ReadInt32();
                    
                    /// SIMULATION INFO
                    r.interfaceh = reader.ReadDouble();
                    r.ROSAVerNr = reader.ReadChars(8);
                    r.ROSAdateTimeID = reader.ReadChars(20);
                    r.ROSACaseName = reader.ReadChars(60);
                    r.Waterdepth = reader.ReadDouble();
                    r.Hs = reader.ReadDouble();
                    r.Hmax = reader.ReadDouble();
                    r.Tp = reader.ReadDouble();
                    r.WaveDir = reader.ReadDouble();
                    r.dT = reader.ReadDouble();
                    r.TotTim = reader.ReadDouble();
                    /// MUDLINE LOAD SUMMARY
                    r.mudline = new List<double>[8];
                    for (int i = 0; i <= 7; i++)
                    {
                        r.mudline[i] = new List<double>();
                        for (int j = 0; j < 4; j++)
                        {
                            r.mudline[i].Add(reader.ReadDouble());
                        }
                    }
                    
                    ///PROGRAM INFO
                    r.JESICAProgName = reader.ReadChars(8);
                    r.JESICAVerNr = reader.ReadChars(8);
                    r.JESICADateTimeID = reader.ReadChars(20);
                    r.smax = reader.ReadDouble();
                    // r.i_n = reader.ReadInt32();
                    /// SKIPPING TO DATA USING OFFSET
                    reader.BaseStream.Seek(r.offset, SeekOrigin.Begin);
                    /// SHIELA PARAMETERS
                    r.parameters.Add("keys", "values");
                    ICollection keys = r.parameters.Keys;
                    ICollection values = r.parameters.Values;
                    for (int i = 0; i < r.paramcount; i++)
                    {
                        keys = reader.ReadChars(12);
                        values = reader.ReadChars(12);
                        r.parameters.Add(keys, values);
                    }
                    r.superstart = r.offset + (r.paramcount * 2 * 12);
                    
                    ///SUPER ELEMENTS MATRICES
                    r.mmat = new double[r.dofcount, r.dofcount];
                    r.dmat = new double[r.dofcount, r.dofcount];
                    r.smat = new double[r.dofcount, r.dofcount];
                    Superelements super = new Superelements();
                    r.mmat = super.supe(r.dofcount, reader);
                    r.superstart += (1 + r.dofcount) * r.dofcount / 2 * 8;
                    r.dmat = super.supe(r.dofcount, reader);
                    r.superstart += (1 + r.dofcount) * r.dofcount / 2 * 8;
                    r.smat = super.supe(r.dofcount, reader);
                    r.superstart += (1 + r.dofcount) * r.dofcount / 2 * 8;
                    
                    ///LOADING PARAMETERS
                    Load_vec lvec = new Load_vec();
                    r.loadvec = new List<double>[r.timestepcount];
                    r.loadvec = lvec.load_read(reader, r.timestepcount, r.loadvec_dim);

                    if(reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        throw new Exception();
                    }
                }
            }
            catch(FileNotFoundException e)
            {
                Console.WriteLine("IO Exception : {0}", e);
            }
            return r;
        }
        public void Reader_test(Reader read,string SEB_text)
        {
            Console.Write("Writing SEB parameters into a text file \n");
            try
            {
                if (File.Exists(SEB_text))
                {
                    File.Delete(SEB_text);
                }
                else
                    File.Create(SEB_text);
                using(FileStream fs = new FileStream(@SEB_text,FileMode.OpenOrCreate,FileAccess.ReadWrite))
                {
                    StreamWriter writer = new StreamWriter(fs);
                    using (writer)
                    {
                        //read=Reading(SEB_path);
                        writer.WriteLine("INITIAL INFO \n");
                        writer.WriteLine();
                        writer.WriteLine("VerNr.: {0}\n", read.filever);
                        writer.WriteLine("NumHeadPar: {0}\n", read.paramcount);
                        writer.WriteLine("DimSysMats: {0}\n", read.dofcount);
                        writer.WriteLine("DimLoadVec: {0}\n", read.loadvec_dim);
                        writer.WriteLine("NumTimeSteps: {0}\n", read.timestepcount);
                        writer.WriteLine("Offset: {0}\n", read.offset);
                        writer.WriteLine();
                        writer.WriteLine("\n SIMULATION INFO \n");
                        writer.WriteLine();
                        writer.WriteLine("Interface Height: {0}\n", read.interfaceh);
                        writer.WriteLine("ROSAVerNr.: {0}\n", new string(read.ROSAVerNr));
                        writer.WriteLine("ROSA DateTime ID: {0}\n", new string(read.ROSAdateTimeID));
                        writer.WriteLine("ROSA Case Name: {0}\n", new string(read.ROSACaseName));
                        writer.WriteLine("Water Depth: {0}\n", read.Waterdepth);
                        writer.WriteLine("Hs: {0}\n", read.Hs);
                        writer.WriteLine("Hmax: {0}\n", read.Hmax);
                        writer.WriteLine("Tp: {0}\n", read.Tp);
                        writer.WriteLine("Wave Dir: {0}\n", read.WaveDir);
                        writer.WriteLine("dT: {0}\n", read.dT);
                        writer.WriteLine("Total Time: {0}\n", read.TotTim);
                        writer.WriteLine();
                        writer.WriteLine("\n MUDLINE LOAD SUMMARY \n");
                        writer.WriteLine();
                        for (int i = 0; i <= 7; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                writer.WriteLine(read.mudline[i][j]);
                                
                            }
                        }
                        writer.WriteLine();
                        writer.WriteLine("\n PROGRAM INFO \n");
                        writer.WriteLine();
                        writer.WriteLine("JESICA Prog Name: {0}\n", new string(read.JESICAProgName));
                        writer.WriteLine("JESICA Version Number: {0}\n", new string(read.JESICAVerNr));
                        writer.WriteLine("JESICA Date Time ID: {0}\n", new string(read.JESICADateTimeID));
                        writer.WriteLine("Maximum sea surface elevation (nmax): {0}\n", read.smax);
                        // writer.WriteLine("Number of Interface Nodes: {0}\n", read.i_n);
                        writer.WriteLine();
                        writer.WriteLine("\n SHIELA PARAMETERS \n");
                        writer.WriteLine();
                        int dict_size = read.parameters.Count;
                        String[] mykeys = new String[dict_size];
                        String[] myvalues = new String[dict_size];
                        read.parameters.Keys.CopyTo(mykeys, 0);
                        read.parameters.Values.CopyTo(myvalues,0);
                        writer.WriteLine("\n  PARAMETER                 VALUE \n");
                        for (int i = 0; i< dict_size; i++)
                        {
                            writer.WriteLine("  {0,-5} {1,-25} {2}", i, mykeys[i],myvalues[i]);
                        }
                        writer.WriteLine();
                        writer.WriteLine("\n MASS PARAMETERS (units kg, kgm, kgm2) \n");
                        writer.WriteLine();
                        for (int i = 0; i < read.dofcount; i++)
                        {
                            for (int j = 0; j < read.dofcount; j++)
                            {
                                writer.Write(string.Format("{0,-25}\t", read.mmat[i, j]));
                            }
                            writer.WriteLine("\n");
                        }
                        writer.WriteLine();
                        writer.WriteLine("\n STIFFNESS PARAMETERS (units N/m, N, Nm) \n");
                        writer.WriteLine();
                        for (int i = 0; i < read.dofcount; i++)
                        {
                            for (int j = 0; j < read.dofcount; j++)
                            {
                                writer.Write(string.Format("{0,-25}\t", read.smat[i, j]));
                            }
                            writer.WriteLine("\n");
                        }
                        writer.WriteLine();
                        writer.WriteLine("\n DAMPING PARAMETERS (units kg/s, kgm/s, kgm2/s) \n");
                        writer.WriteLine();
                        for (int i = 0; i < read.dofcount; i++)
                        {
                            for (int j = 0; j < read.dofcount; j++)
                            {
                                writer.Write(string.Format("{0,-25}\t", read.dmat[i, j]));
                            }
                            writer.WriteLine();
                        }
                        writer.WriteLine();
                        writer.WriteLine("\n LOADING PARAMETERS (units N, Nm)  \n");
                        writer.WriteLine();
                        for(int i=0; i<read.timestepcount;i++)
                        {
                            for(int j=0;j<(read.dofcount+2);j++)
                            {
                                writer.Write(string.Format("{0,-25}", read.loadvec[i][j]));
                                
                            }
                            writer.WriteLine();
                        }
                        writer.Flush();
                    }
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
