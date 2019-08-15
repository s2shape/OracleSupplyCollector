using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using S2.BlackSwan.SupplyCollector;
using S2.BlackSwan.SupplyCollector.Models;

namespace OracleSupplyCollector
{
    public class OracleSupplyCollector : SupplyCollectorBase
    {
        public override List<string> DataStoreTypes() {
            return (new[] { "Oracle" }).ToList();
        }

        public string BuildConnectionString(string user, string password, string database, string host, int port = 1521) {
            return $"Data Source={host}:{port}/{database};User Id={user};Password={password};";
        }

        public override List<string> CollectSample(DataEntity dataEntity, int sampleSize) {
            var result = new List<string>();
            using (var conn = new OracleConnection(dataEntity.Container.ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT {dataEntity.Name} FROM {dataEntity.Collection.Name} where rownum<={sampleSize}";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var val = reader[0];
                            if (val is DBNull)
                            {
                                result.Add(null);
                            }
                            else
                            {
                                result.Add(val.ToString());
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Warning: requires running ANALYZE TABLE {tableName} COMPUTE STATISTICS;
        /// </summary>
        public override List<DataCollectionMetrics> GetDataCollectionMetrics(DataContainer container) {
            var metrics = new List<DataCollectionMetrics>();

            using (var conn = new OracleConnection(container.ConnectionString)) {
                conn.Open();

                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "select owner, table_name, num_rows, blocks, empty_blocks, avg_space from all_tables t where t.owner not in ('SYS') and t.tablespace_name not in ('SYSAUX')";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int column = 0;

                            var schema = reader.GetString(column++);
                            var table = reader.GetString(column++);
                            var rows = reader.GetDbInt64(column++);
                            var usedBlocks = reader.GetDbInt64(column++);
                            var emptyBlocks = reader.GetDbInt64(column++);
                            var blockSize = reader.GetDbInt64(column++);

                            metrics.Add(new DataCollectionMetrics()
                            {
                                Schema = schema,
                                Name = table,
                                RowCount = rows,
                                TotalSpaceKB = (decimal)(usedBlocks + emptyBlocks) * blockSize / 1024,
                                UnUsedSpaceKB = (decimal)emptyBlocks * blockSize / 1024,
                                UsedSpaceKB = (decimal)usedBlocks * blockSize / 1024,
                            });
                        }
                    }
                }
            }

            return metrics;
        }

        private DataType ConvertDataType(string dbDataType, int len, int precision) {
            if ("char".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.Char;
            } else if ("varchar".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.String;
            } else if ("varchar2".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.String;
            } else if ("nvarchar2".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.String;
            } else if ("number".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                if (precision == 0) {
                    if (len > 10) {
                        return DataType.Long;
                    } else if (len > 5) {
                        return DataType.Int;
                    } else if (len > 1) {
                        return DataType.Short;
                    } else {
                        return DataType.Boolean;
                    }
                }
                return DataType.Decimal;
            } else if ("date".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.DateTime;
            } else if ("timestamp".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.DateTime;
            } else if ("timestamp with time zone".Equals(dbDataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.DateTime;
            }

            return DataType.Unknown;
        }

        public override (List<DataCollection>, List<DataEntity>) GetSchema(DataContainer container) {
            var collections = new List<DataCollection>();
            var entities = new List<DataEntity>();

            using (var conn = new OracleConnection(container.ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT \n" +
                        "  c.owner, c.table_name, c.column_name, c.data_type, c.data_length, c.data_precision,\n" +
                        "(SELECT COUNT(*) FROM ALL_CONSTRAINTS ac, ALL_CONS_COLUMNS acc\n" +
                        "   where acc.constraint_name=ac.constraint_name and ac.owner=acc.owner and ac.table_name=acc.table_name and " +
                        "   ac.owner=c.owner and ac.table_name=c.table_name and acc.column_name=c.column_name and ac.constraint_type='P'" +
                        ") AS IsPrimary,\n" +                        
                        "(SELECT COUNT(*) FROM ALL_CONSTRAINTS ac, ALL_CONS_COLUMNS acc\n" +
                        "   where acc.constraint_name=ac.constraint_name and ac.owner=acc.owner and ac.table_name=acc.table_name and " +
                        "   ac.owner=c.owner and ac.table_name=c.table_name and acc.column_name=c.column_name and ac.constraint_type='U'" +
                        ") AS IsUnique,\n" +
                        "(SELECT COUNT(*) FROM ALL_CONSTRAINTS ac, ALL_CONS_COLUMNS acc\n" +
                        "   where acc.constraint_name=ac.constraint_name and ac.owner=acc.owner and ac.table_name=acc.table_name and " +
                        "   ac.owner=c.owner and ac.table_name=c.table_name and acc.column_name=c.column_name and ac.constraint_type='R'" +
                        ") AS IsRef,\n" +
                        "(SELECT COUNT(*) FROM ALL_IND_COLUMNS aic\n" +
                        "   where aic.table_owner=c.owner and aic.table_name=c.table_name and aic.column_name=c.column_name" +
                        ") AS IsIndexed\n" +
                        "FROM ALL_TAB_COLUMNS c, ALL_TABLES t\n" +
                        "where c.table_name = t.table_name and c.owner = t.owner and t.owner not in ('SYS') and t.tablespace_name not in ('SYSAUX')";

                    DataCollection collection = null;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int column = 0;

                            var owner = reader.GetDbString(column++);
                            var table = reader.GetDbString(column++);
                            var columnName = reader.GetDbString(column++);
                            var dataType = reader.GetDbString(column++);
                            var dataLen = reader.GetInt32(column++);
                            var dataPrecision = reader.GetDbInt32(column++);
                            var isPrimary = reader.GetInt32(column++) > 0;
                            var isUnique = reader.GetInt32(column++) > 0;
                            var isRef = reader.GetInt32(column++) > 0;
                            var isIndexed = reader.GetInt32(column++) > 0;

                            if (collection == null || !collection.Schema.Equals(owner) ||
                                !collection.Name.Equals(table))
                            {

                                collection = new DataCollection(container, table)
                                {
                                    Schema = owner
                                };
                                collections.Add(collection);
                            }

                            entities.Add(new DataEntity(columnName, ConvertDataType(dataType, dataLen, dataPrecision), dataType, container,
                                collection)
                            {
                                IsAutoNumber = false, // Sequences are used in Oracle
                                IsComputed = false,
                                IsForeignKey = isRef,
                                IsIndexed = isIndexed,
                                IsPrimaryKey = isPrimary,
                                IsUniqueKey = isUnique
                            });

                            if (!reader.HasRows) {
                                break;
                            }
                        }
                    }
                }
            }

            return (collections, entities);
        }

        public override bool TestConnection(DataContainer container) {
            try
            {
                using (var conn = new OracleConnection(container.ConnectionString))
                {
                    conn.Open();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal static class DbDataReaderExtensions
    {
        internal static string GetDbString(this DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return null;
            return reader.GetString(ordinal);
        }
        internal static int GetDbInt32(this DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return 0;
            return reader.GetInt32(ordinal);
        }
        internal static long GetDbInt64(this DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return 0;
            return reader.GetInt64(ordinal);
        }
    }
}
