using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace IntegrationEventProducer
{
    public class ChangeReader
    {
        private string _connectionString;
        private List<string> _columns = null;
        private byte[] _lastReadPosition = null;
        private byte[] _nextReadPosition = null;
        private string _tableName;
        private string _schemaName;

        public ChangeReader(IConfiguration configuration, string schemaName, string tableName)
        {
            _connectionString = configuration.GetConnectionString("Default");
            _schemaName = schemaName;
            _tableName = tableName;
        }


        public async Task<List<string>> GetList()
        {

            List<string> items = new List<string>();
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    if (_lastReadPosition == null)
                    {
                        try
                        {
                            var maxLsnCmd = new SqlCommand("select sys.fn_cdc_get_max_lsn()", con);
                            _lastReadPosition = (byte[])maxLsnCmd.ExecuteScalar();
                        }
                        catch
                        {
                            return items;
                        }
                    }

                    SqlCommand cmd = new SqlCommand("SELECT * FROM cdc.fn_cdc_get_all_changes_" + _schemaName +  "_" + _tableName + " (@from_lsn, sys.fn_cdc_get_max_lsn(), N'all')", con);
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@from_lsn";
                    param.Value = _lastReadPosition;
                    cmd.Parameters.Add(param);

                    cmd.CommandType = CommandType.Text;
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    var columns = GetColumns(reader);
                    _nextReadPosition = null;
                    while (reader.Read())
                    {
                        _nextReadPosition = (byte[])reader["__$start_lsn"];
                        if (!StructuralComparisons.StructuralEqualityComparer.Equals(_nextReadPosition, _lastReadPosition))
                        {
                            var row = SerializeRow(columns, reader);
                            items.Add(JsonConvert.SerializeObject(row, Formatting.None));
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return items;
        }

        public void ApplyNextReadPosition()
        {
            if (_nextReadPosition != null)
            {
                // This should be persisted to survive crashes
                _lastReadPosition = _nextReadPosition;
            }
        }

        private byte[] EnsureLastReadPosition()
        {
            if (_lastReadPosition == null)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(_connectionString))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("select sys.fn_cdc_get_max_lsn()", con);
                        _lastReadPosition = (byte[])cmd.ExecuteScalar();
                    }

                }
                catch 
                {
                }
            }

            return _lastReadPosition;
        }

        private List<string> GetColumns(SqlDataReader reader)
        {
            if (_columns == null)
            {
                var cols = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    string name = reader.GetName(i);
                    if (!name.StartsWith("__$"))
                    {
                        cols.Add(reader.GetName(i));
                    }
                }

                _columns = cols;
            }

            return _columns;
        }

        private Dictionary<string, object> SerializeRow(List<string> cols,
            SqlDataReader reader)
        {
            byte[] maskBytes = (byte[])reader["__$update_mask"];
            var mask = BitConverter.ToInt64(To8Bytes(maskBytes));

            int operation = (int)reader["__$operation"];
            string type = TypeFromOperation(operation);
            var result = new Dictionary<string, object>();
            long colMaskValue = 1;
            List<string> updatedFields = new List<string>();
            for (int ind = 0; ind < cols.Count; ind++)
            {
                var col = cols[ind];
                if (type == "update" && (colMaskValue & mask) > 0)
                {
                    updatedFields.Add(col);
                }
                result.Add(col, reader[col]);

                colMaskValue *= 2;
            }

            var integrationEvent = new Dictionary<string, object>();
            integrationEvent.Add("type", _tableName + "_" + type);
            integrationEvent.Add("timestamp", DateTime.Now);
            if (type == "update")
            {
                integrationEvent.Add("updatedFields", updatedFields);
            }
            integrationEvent.Add("data", result);

            return integrationEvent;
        }

        private string TypeFromOperation(int operation)
        {
            switch (operation)
            {
                case 1:
                    return "delete";
                case 2:
                    return "insert";
                case 4:
                    return "update";
                default:
                    throw new Exception("Invalid operation: " + operation);
            }

        }

        private byte[] To8Bytes(byte[] bytes)
        {
            byte[] update8Bytes = new byte[8];
            for (int ind = 0; ind < bytes.Length; ind++)
            {
                update8Bytes[ind] = bytes[bytes.Length - ind - 1];
            }

            for (int ind = bytes.Length; ind < update8Bytes.Length; ind++)
            {
                update8Bytes[ind] = 0;
            }

            return update8Bytes;
        }
    }
}
