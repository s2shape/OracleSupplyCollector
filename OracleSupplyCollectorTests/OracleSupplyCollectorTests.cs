using System;
using System.Collections.Generic;
using System.Linq;
using S2.BlackSwan.SupplyCollector.Models;
using Xunit;

namespace OracleSupplyCollectorTests
{
    public class OracleSupplyCollectorTests : IClassFixture<LaunchSettingsFixture>
    {
        private readonly OracleSupplyCollector.OracleSupplyCollector _instance;
        private readonly DataContainer _container;
        private LaunchSettingsFixture _fixture;

        public OracleSupplyCollectorTests(LaunchSettingsFixture fixture) {
            _fixture = fixture;
            _instance = new OracleSupplyCollector.OracleSupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString(
                    Environment.GetEnvironmentVariable("ORACLE_USER"),
                    Environment.GetEnvironmentVariable("ORACLE_PASSWORD"),
                    Environment.GetEnvironmentVariable("ORACLE_SID"),
                    Environment.GetEnvironmentVariable("ORACLE_HOST")
                    )
            };
        }

        [Fact]
        public void DataStoreTypesTest()
        {
            var result = _instance.DataStoreTypes();
            Assert.Contains("Oracle", result);
        }

        [Fact]
        public void TestConnectionTest()
        {
            var result = _instance.TestConnection(_container);
            Assert.True(result);
        }

        [Fact]
        public void GetDataCollectionMetricsTest()
        {
            var metrics = new DataCollectionMetrics[] {
                new DataCollectionMetrics()
                    {Name = "TEST_DATA_TYPES", RowCount = 1, TotalSpaceKB = 54.47M},
                new DataCollectionMetrics()
                    {Name = "TEST_FIELD_NAMES", RowCount = 1, TotalSpaceKB = 55.08M},
                new DataCollectionMetrics()
                    {Name = "TEST_INDEX", RowCount = 7, TotalSpaceKB = 54.45M},
                new DataCollectionMetrics()
                    {Name = "TEST_INDEX_REF", RowCount = 2, TotalSpaceKB = 55.07M}
            };

            var result = _instance.GetDataCollectionMetrics(_container);
            result = result.Where(x => x.Name.StartsWith("TEST_")).ToList();

            Assert.Equal(metrics.Length, result.Count);

            foreach (var metric in metrics)
            {
                var resultMetric = result.Find(x => x.Name.Equals(metric.Name));
                Assert.NotNull(resultMetric);

                Assert.Equal(metric.RowCount, resultMetric.RowCount);
                Assert.Equal(metric.TotalSpaceKB, resultMetric.TotalSpaceKB, 2);
            }
        }

        [Fact]
        public void GetTableNamesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            tables = tables.Where(x => x.Name.StartsWith("test_", StringComparison.InvariantCultureIgnoreCase)).ToList();
            elements = elements.Where(x => x.Collection.Name.StartsWith("test_", StringComparison.InvariantCultureIgnoreCase)).ToList();

            Assert.Equal(4, tables.Count);
            Assert.Equal(20, elements.Count);

            var tableNames = new string[] { "TEST_DATA_TYPES", "TEST_FIELD_NAMES", "TEST_INDEX", "TEST_INDEX_REF" };
            foreach (var tableName in tableNames)
            {
                var table = tables.Find(x => x.Name.Equals(tableName));
                Assert.NotNull(table);
            }
        }

        [Fact]
        public void DataTypesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            var dataTypes = new Dictionary<string, string>() {
                {"ID", "NUMBER"},
                {"CHAR_FIELD", "CHAR"},
                {"VARCHAR_FIELD", "VARCHAR2"},
                {"VARCHAR2_FIELD", "VARCHAR2"},
                {"NVARCHAR2_FIELD", "NVARCHAR2"},
                {"NUMBER_FIELD", "NUMBER"},
                {"DATE_FIELD", "DATE"},
                {"TIMESTAMP_FIELD", "TIMESTAMP(6)"},
                {"TIMESTAMPTZ_FIELD", "TIMESTAMP(6) WITH TIME ZONE"},
            };

            var columns = elements.Where(x => x.Collection.Name.Equals("TEST_DATA_TYPES")).ToArray();
            Assert.Equal(9, columns.Length);

            foreach (var column in columns)
            {
                Assert.Contains(column.Name, (IDictionary<string, string>)dataTypes);
                Assert.Equal(dataTypes[column.Name], column.DbDataType);
            }
        }

        [Fact]
        public void SpecialFieldNamesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            var fieldNames = new string[] { "ID", "LOW_CASE", "UPCASE", "CAMELCASE", "Table", "array", "SELECT" };

            var columns = elements.Where(x => x.Collection.Name.Equals("TEST_FIELD_NAMES")).ToArray();
            Assert.Equal(fieldNames.Length, columns.Length);

            foreach (var column in columns)
            {
                Assert.Contains(column.Name, fieldNames);
            }
        }

        [Fact]
        public void AttributesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            tables = tables.Where(x => x.Name.StartsWith("test_", StringComparison.InvariantCultureIgnoreCase)).ToList();
            elements = elements.Where(x => x.Collection.Name.StartsWith("test_", StringComparison.InvariantCultureIgnoreCase)).ToList();

            var idFields = elements.Where(x => x.Name.Equals("ID")).ToArray();
            Assert.Equal(4, idFields.Length);

            foreach (var idField in idFields)
            {
                Assert.Equal(DataType.Long, idField.DataType);
                Assert.True(idField.IsPrimaryKey);
            }

            var uniqueField = elements.Find(x => x.Name.Equals("NAME"));
            Assert.True(uniqueField.IsUniqueKey);

            var refField = elements.Find(x => x.Name.Equals("INDEX_ID"));
            Assert.True(refField.IsForeignKey);

            foreach (var column in elements)
            {
                if (column.Name.Equals("ID") || column.Name.Equals("NAME") || column.Name.Equals("INDEX_ID"))
                {
                    continue;
                }

                Assert.False(column.IsPrimaryKey);
                Assert.False(column.IsAutoNumber);
                Assert.False(column.IsForeignKey);
                Assert.False(column.IsIndexed);
            }
        }

        [Fact]
        public void CollectSampleTest()
        {
            var entity = new DataEntity("name", DataType.String, "character varying", _container,
                new DataCollection(_container, "test_index"));

            var samples = _instance.CollectSample(entity, 5);
            Assert.InRange(samples.Count, 4, 6);

            var all_samples = _instance.CollectSample(entity, 7);
            Assert.InRange(all_samples.Count, 4, 8);

        }
    }
}
