using Dapper;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Serialization;

public class ExecuteProcedure
{
    public List<DataVM> GetData()
    {
        var parameters = new
        {
            Parameter1 = parameter1,
            Parameter2 = parameter2,
            Parameter3 = parameter3,
            Parameter4 = parameter4,
            Parameter5 = parameter5
        };

        var dataVMs = ExecuteProcedure<DataVM>("procedureName", parameters, timeout: 600);

        return (dataVMs);
    }

    public string ChangeData()
    {
        using (var transaction = this.UnitOfWork.Database.BeginTransaction())
        {
            var parameters = new
            {
                Parameter1 = parameter1,
                Parameter2 = parameter2,
                Parameter3 = parameter3,
                Parameter4 = parameter4,
                Parameter5 = parameter5
            };

            var check = ExecuteProcedure<bool>("procedureName", parameters, transaction: transaction.UnderlyingTransaction, timeout: 600).FirstOrDefault();

            if (check == true)
            {
                transaction.Commit();
                retunt "Success";
            }
            else
            {
                transaction.Rollback();
                retunt "Error";
            }
        }
    }

    private IEnumerable<T> ExecuteProcedure<T>(string procedureName, object parameters, DbTransaction transaction = null, int timeout = 300)
    {
        var connectionString = "//Connection String";

        // Using EF because of the transaction
        if (transaction != null)
        {
            List<T> results = new List<T>();

            DbCommand dbCommand = transaction.Connection.CreateCommand();
            dbCommand.CommandType = CommandType.StoredProcedure;
            dbCommand.Transaction = transaction;
            dbCommand.CommandText = procedureName;
            dbCommand.CommandTimeout = timeout;

            foreach (var parameter in parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var dbParameter = dbCommand.CreateParameter();
                dbParameter.ParameterName = parameter.Name;
                dbParameter.Value = parameter.GetValue(parameters, null);

                dbCommand.Parameters.Add(dbParameter);
            }

            using (var reader = dbCommand.ExecuteReader())
            {
                var resultType = typeof(T);

                T result = (T)FormatterServices.GetUninitializedObject(resultType);

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (resultType.BaseType.Name == "ValueType")
                        {
                            result = (T)Convert.ChangeType(reader[i], typeof(T));
                        }
                        else
                        {
                            result.GetType().GetProperty(reader.GetName(i)).SetValue(reader[i], 0);
                        }
                    }

                    results.Add(result);
                }
            }

            return results;
        }
        // using Dapper
        else
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var results = connection.Query<T>(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeout);

                return results;
            }
        }
    }
}