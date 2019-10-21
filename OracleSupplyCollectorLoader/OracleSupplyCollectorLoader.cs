using System;
using System.IO;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using S2.BlackSwan.SupplyCollector.Models;
using SupplyCollectorDataLoader;

namespace OracleSupplyCollectorLoader
{
    public class OracleSupplyCollectorLoader : SupplyCollectorDataLoaderBase
    {
        public override void InitializeDatabase(DataContainer dataContainer) {
            
        }

        public override void LoadSamples(DataEntity[] dataEntities, long count) {
            using (var conn = new OracleConnection(dataEntities[0].Container.ConnectionString))
            {
                conn.Open();

                var sb = new StringBuilder();
                sb.Append("CREATE TABLE ");
                sb.Append(dataEntities[0].Collection.Name);
                sb.Append(" (\n");
                sb.Append("id_field integer not null");

                foreach (var dataEntity in dataEntities)
                {
                    sb.Append(",\n");
                    sb.Append(dataEntity.Name);
                    sb.Append(" ");

                    switch (dataEntity.DataType)
                    {
                        case DataType.String:
                            sb.Append("varchar2(1024)");
                            break;
                        case DataType.Int:
                            sb.Append("integer");
                            break;
                        case DataType.Double:
                            sb.Append("number");
                            break;
                        case DataType.Boolean:
                            sb.Append("number(1)");
                            break;
                        case DataType.DateTime:
                            sb.Append("timestamp");
                            break;
                        default:
                            sb.Append("integer");
                            break;
                    }

                    sb.AppendLine();
                }

                sb.Append(")");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                }

                var r = new Random();
                long rows = 0;
                while (rows < count)
                {
                    long bulkSize = 1000;
                    if (bulkSize + rows > count)
                        bulkSize = count - rows;

                    sb = new StringBuilder();
                    sb.Append("INSERT INTO ");
                    sb.Append(dataEntities[0].Collection.Name);
                    sb.Append("(id_field");

                    
                    foreach (var dataEntity in dataEntities)
                    {
                        sb.Append(", ");
                        sb.Append(dataEntity.Name);
                    }
                    sb.Append(") ");

                    for (int i = 0; i < bulkSize; i++)
                    {
                        if (i > 0) {
                            sb.Append("union all ");
                        }

                        sb.Append("select ");
                        sb.Append(rows + i);
                        foreach (var dataEntity in dataEntities)
                        {
                            sb.Append(", ");

                            switch (dataEntity.DataType)
                            {
                                case DataType.String:
                                    sb.Append("'");
                                    sb.Append(new Guid().ToString());
                                    sb.Append("'");
                                    break;
                                case DataType.Int:
                                    sb.Append(r.Next().ToString());
                                    break;
                                case DataType.Double:
                                    sb.Append(r.NextDouble().ToString().Replace(",", "."));
                                    break;
                                case DataType.Boolean:
                                    sb.Append(r.Next(100) > 50 ? "1" : "0");
                                    break;
                                case DataType.DateTime:
                                    var val = DateTimeOffset
                                        .FromUnixTimeMilliseconds(
                                            DateTimeOffset.Now.ToUnixTimeMilliseconds() + r.Next()).DateTime;
                                    sb.Append("'");
                                    sb.Append(val.ToString("s"));
                                    sb.Append("'");
                                    break;
                                default:
                                    sb.Append(r.Next().ToString());
                                    break;
                            }
                        }

                        sb.AppendLine(" from dual");
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sb.ToString();
                        cmd.ExecuteNonQuery();
                    }

                    rows += bulkSize;
                    Console.Write(".");
                }

                Console.WriteLine();
            }

        }

        public override void LoadUnitTestData(DataContainer dataContainer) {
            using (var conn = new OracleConnection(dataContainer.ConnectionString))
            {
                conn.Open();

                using (var reader = new StreamReader("tests/data.sql"))
                {
                    var sb = new StringBuilder();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (String.IsNullOrEmpty(line))
                            continue;

                        if(line.Equals("quit;", StringComparison.InvariantCultureIgnoreCase) || line.Equals("/"))
                            continue;

                        sb.AppendLine(line);
                        if (line.TrimEnd().EndsWith(";"))
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                Console.WriteLine(sb.ToString());

                                cmd.CommandTimeout = 600;
                                cmd.CommandText = sb.ToString().TrimEnd(new []{' ', '\n', '\r', '\t', ';'});

                                cmd.ExecuteNonQuery();
                            }

                            sb.Clear();
                        }
                    }
                }
            }

        }
    }
}
