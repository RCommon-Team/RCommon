using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="IDataReader"/> including safe value retrieval
    /// and conversion to <see cref="DataTable"/>.
    /// </summary>
    public static class IDataReaderExtensions
    {

		/// <summary>
		/// Acquires the value out of a row of a IDataReader based on the column name.
		/// </summary>
		/// <param name="dr">A populated IDataReader (DataReader)</param>
		/// <param name="index">The index of the column to retrieve the value from</param>
		/// <param name="defaultValue">The default value of the column should the column not be found.</param>
		/// <returns>The value of the object</returns>
		/// <remarks>If the index is not found, then this method throws an exception and assigns the default value.</remarks>
		public static object GetValue(this IDataReader dr, int index, object defaultValue)
		{
			object rv = defaultValue;

			try
			{
				rv = dr.GetValue(index);

				if (rv == DBNull.Value)
				{
					rv = defaultValue;
				}
			}
			catch (Exception)
			{

				rv = defaultValue;
			}
			return rv;
		}

		/// <summary>
		/// Acquires the value from a row of an <see cref="IDataReader"/> based on the column name.
		/// </summary>
		/// <param name="dr">A populated <see cref="IDataReader"/>.</param>
		/// <param name="columnName">The name of the column to retrieve the value from.</param>
		/// <param name="defaultValue">The default value to return if the column is not found or is <see cref="DBNull"/>.</param>
		/// <returns>The value of the column, or <paramref name="defaultValue"/> if not found or null.</returns>
		public static object GetValue(this IDataReader dr, string columnName, object defaultValue)
		{
			object rv = defaultValue;

			try
			{
				int index = dr.GetOrdinal(columnName);
				rv = dr.GetValue(index);

				if (rv == DBNull.Value)
				{
					rv = defaultValue;
				}
			}
			catch (Exception)
			{

				rv = defaultValue;
			}
			return rv;
		}

		/// <summary>
		/// Converts a DataReader Interface (IDataReader) to a DataTable
		/// </summary>
		/// <param name="dr">Data Reader Interface containing the data</param>
		/// <returns>A populated DataTable</returns>
		/// <remarks>This method does not close the IDataReader.  You will have to.</remarks>
		public static DataTable ToDataTable(this IDataReader dr)
		{
			DataTable? dtSchema = dr.GetSchemaTable();
			DataTable dtData = new DataTable();
			DataColumn dc;
			DataRow row;
			System.Collections.ArrayList al = new System.Collections.ArrayList();

			if (dtSchema == null) return dtData;

			// Populate the Column Information
			for (int i = 0; i < dtSchema.Rows.Count; i++)
			{
				dc = new DataColumn();

				var columnName = dtSchema.Rows[i]["ColumnName"]?.ToString();
				if (columnName != null && !dtData.Columns.Contains(columnName))
				{
					dc.ColumnName = columnName;
					dc.Unique = Convert.ToBoolean(dtSchema.Rows[i]["IsUnique"]);
					dc.AllowDBNull = Convert.ToBoolean(dtSchema.Rows[i]["AllowDBNull"]);
					dc.ReadOnly = Convert.ToBoolean(dtSchema.Rows[i]["IsReadOnly"]);
					dc.DataType = (Type?)dtSchema.Rows[i]["DataType"] ?? typeof(object);
					al.Add(dc.ColumnName);

					dtData.Columns.Add(dc);
				}
			}

			// Loop through the data
			while (dr.Read())
			{
				row = dtData.NewRow();

				for (int i = 0; i < al.Count; i++)
				{
					row[((string)al[i]!)] = dr[(string)al[i]!];
				}

				dtData.Rows.Add(row);
			}
			dr.Close();

			return dtData;
		}

		/// <summary>
		/// Converts a DataReader Interface (IDataReader) to a DataTable
		/// </summary>
		/// <param name="dr">Data Reader Interface containing the data</param>
		/// <param name="destroyReader">Determines weather or not to destory the IDataReader after the DataTable has been populated.</param>
		/// <returns>A populated DataTable</returns>
		public static DataTable ToDataTable(this IDataReader dr, bool destroyReader)
		{
			try
			{
				DataTable? dtSchema = dr.GetSchemaTable();
				DataTable dtData = new DataTable();
				DataColumn dc;
				DataRow row;
				System.Collections.ArrayList al = new System.Collections.ArrayList();

				if (dtSchema == null) return dtData;

				// Populate the Column Information
				for (int i = 0; i < dtSchema.Rows.Count; i++)
				{
					dc = new DataColumn();

					var columnName2 = dtSchema.Rows[i]["ColumnName"]?.ToString();
					if (columnName2 != null && !dtData.Columns.Contains(columnName2))
					{
						dc.ColumnName = columnName2;
						dc.Unique = Convert.ToBoolean(dtSchema.Rows[i]["IsUnique"]);
						dc.AllowDBNull = Convert.ToBoolean(dtSchema.Rows[i]["AllowDBNull"]);
						dc.ReadOnly = Convert.ToBoolean(dtSchema.Rows[i]["IsReadOnly"]);
						al.Add(dc.ColumnName);
						dtData.Columns.Add(dc);
					}
				}

				// Loop through the data
				while (dr.Read())
				{
					row = dtData.NewRow();

					for (int i = 0; i < al.Count; i++)
					{
						row[((string)al[i]!)] = dr[(string)al[i]!];
					}

					dtData.Rows.Add(row);
				}



				return dtData;
			}
			catch (Exception)
			{

				throw;
			}
			finally
			{
				if (destroyReader && !dr.IsClosed)
				{
					dr.Close();
				}
			}
		}
    }
}
