﻿using ClinicManagementSystem.Model;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static ClinicManagementSystem.Service.DataAccess.IDao;

namespace ClinicManagementSystem.Service.DataAccess
{
    public class SqlServerDao : IDao
    {
        public Tuple<List<User>, int> GetUsers(
        int page, int rowsPerPage,
        string keyword,
        Dictionary<string, SortType> sortOptions)
        {
            var result = new List<User>();
            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
            string sortString = "ORDER BY ";
            bool useDefault = true;
            foreach (var item in sortOptions)
            {
                useDefault = false;
                if (item.Key == "Name")
                {
                    if (item.Value == SortType.Ascending)
                    {
                        sortString += "Name asc ";
                    }
                    else
                    {
                        sortString += "Name desc ";
                    }
                }
            }
            if (useDefault)
            {
                sortString += "ID ";
            }


            var sql = $"""
            SELECT count(*) over() as Total, id, name, role, username,password,phone,birthday,address,gender,entropy
            FROM EndUser
            WHERE Name like @Keyword
            {sortString} 
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;        
        """;
            var command = new SqlCommand(sql, connection);
            AddParameters(command, ("@Skip", (page - 1) * rowsPerPage), ("@Take", rowsPerPage), ("@Keyword", $"%{keyword}%"));
            var reader = command.ExecuteReader();
            int count = -1;
            string entropy = "";
            while (reader.Read())
            {
                if (count == -1)
                {
                    count = (int)reader["Total"];
                }
                var user = new User();
                user.id = (int)reader["id"];
                user.name = (string)reader["name"];
                user.username = (string)reader["username"];
                user.gender = (string)reader["gender"];
                user.role = (string)reader["role"];
                user.birthday = (DateTime)reader["birthday"];
                user.address = (string)reader["address"];
                user.phone = (string)reader["phone"];
                entropy = (string)reader["entropy"];
                user.password = DecryptionPassword((string)reader["password"], entropy);
                result.Add(user);
            }

            connection.Close();
            return new Tuple<List<User>, int>(
                result, count
            );
        }
        private static string GetConnectionString()
        {
            var connectionString = """
                    Server = localhost,1433;
                    Database = ClinicManagementSystemDatabase;
                    User Id = sa;
                    Password = SqlServer@123;
                    TrustServerCertificate = True;
                """;
            return connectionString;
        }
        private readonly string _connectionString = GetConnectionString();
        public (int,string,string,string,string,string) Authentication(string username , string password )
        {

            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string query = $"SELECT id,name, role,password,phone,birthday,gender,address,entropy FROM EndUser WHERE username = @Username";
            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            var reader = command.ExecuteReader();

            int id = 0;
            string name = "";
            string role = "";
            //string encryptedPasswordInBase64 = "";
            string passwordFromDB = "";

            string phone = "";
            DateTime birthday;
            string gender;
            string address;
            //string entropyInBase64 = "";
            if (reader.Read())
            {
                id = (int)reader["id"];
                name = (string)reader["name"];
                role = reader["role"].ToString();
                //encryptedPasswordInBase64 = reader["password"].ToString();
                passwordFromDB = reader["password"].ToString();

                phone = reader["phone"].ToString();
                birthday = (DateTime)reader["birthday"];
                gender = reader["gender"].ToString();
                address = reader["address"].ToString();
                //entropyInBase64 = reader["entropy"].ToString();
                connection.Close();
                //var encryptedPasswordInBytes = Convert.FromBase64String(encryptedPasswordInBase64);
                //var entropyInBytes = Convert.FromBase64String(entropyInBase64);
                //var passwordInBytes = ProtectedData.Unprotect(encryptedPasswordInBytes, entropyInBytes, DataProtectionScope.CurrentUser);
                //var passwordGetFromDatabase = Encoding.UTF8.GetString(passwordInBytes);
                if (password == passwordFromDB)
                {
                    return (id, name, role, phone, gender, address);
                }
                return (0, "", "", "", "", "");
            }
            else
            {
                connection.Close();
                return (0, "", "", "", "", "");
            }
            //return (0, "", "admin", "", "", "");
        }
        public bool CheckUserExists(string username)
        {
            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string query = $"SELECT count(*) FROM EndUser WHERE username = @Username";
            var command = new SqlCommand(query, connection);
            AddParameters(command,
               ("@Username", username)
               );
            int count = (int)command.ExecuteScalar();
            return count > 0;
        }
        public bool CreateUser(User user)
        {
            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string query = $"insert into EndUser(name,role,username,password,phone,birthday,address,gender) values(@name,@role,@username,@password,@phone,@birthday,@address,@gender) ";
            var command = new SqlCommand(query, connection);
            AddParameters(command, 
                ("@name", user.name),
                ("@role", user.role),
                ("@username", user.username),
                ("@password", user.password),
                ("@phone", user.phone),
                ("@birthday", user.birthday),
                ("@address", user.address),
                ("@gender", user.gender)
                //("@entropy", entropy)
                );

            int count = command.ExecuteNonQuery();
            bool success = count == 1;
            connection.Close();
            return success;
        }
        public bool UpdateUser(User info, string entropyUserEdit)
        {
            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            var sql = "update EndUser set name=@name, role=@role,username=@username,password=@password,phone=@phone,birthday=@birthday,address=@address,gender=@gender,entropy=@entropy where id=@id";
            var command = new SqlCommand(sql, connection);
            AddParameters(command,
                ("@id", info.id),
                ("@name", info.name),
                ("@role", info.role),
                ("@username", info.username),
                ("@password", info.password),
                ("@phone", info.phone),
                ("@birthday", info.birthday),
                ("@address", info.address),
                ("@gender", info.gender),
                ("@entropy", entropyUserEdit)
                );
            int count = command.ExecuteNonQuery();
            bool success = count == 1;

            connection.Close();
            return success;
        }

        public List<MedicalExaminationForm> GetMedicalExaminationForms()
        {
            var forms = new List<MedicalExaminationForm>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, PatientId, StaffId, Time, Symptom, DoctorId FROM MedicalExaminationForm", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var form = new MedicalExaminationForm
                        {
                            Id = reader.GetInt32(0),                                                    // Id
                            PatientId = reader.GetInt32(1),                                             // PatientId
                            StaffId = reader.GetInt32(2),                                               // StaffId
                            Time = reader.GetDateTime(3),                                               // Time
                            Symptoms = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),          // Symptom
                            DoctorId = reader.GetInt32(5)                                               // DoctorId
                        };

                        forms.Add(form);
                    }
                }
            }

            return forms;
        }
        
		private void AddParameters(SqlCommand command, params (string ParameterName, object Value)[] parameters)
		{
			foreach (var (parameterName, value) in parameters)
			{
				command.Parameters.AddWithValue(parameterName, value);
			}
		}


		public (bool, int) AddPatient(Patient patient)
        {
            var connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

			

            var command = new SqlCommand("INSERT INTO Patient (Name , Email , ResidentId, Address , Birthday , Gender) " +
				"VALUES (@Name , @Email , @ResidentId , @Address , @DoB , @Gender)" +
				"SELECT CAST(SCOPE_IDENTITY() AS int);", connection);
            AddParameters(command,
                ("@Name", patient.Name),
                ("@Email", patient.Email),
                ("@ResidentId", patient.ResidentId),
                ("@Address", patient.Address),
                ("@DoB", patient.DoB),
                ("@Gender", patient.Gender));

			var result = command.ExecuteScalar();
			int id = result != null ? Convert.ToInt32(result) : 0;

			connection.Close();
			return (id > 0, id);
        }

		public (bool, int) checkPatientExists(string residentId)
		{
			var connectionString = GetConnectionString();
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();

			string query = "SELECT id FROM Patient WHERE ResidentId = @ResidentId";
			var command = new SqlCommand(query, connection);
			AddParameters(command, 
				("@ResidentId", residentId));

			
			var reader = command.ExecuteReader();
			if(reader.Read())
			{
				int id = (int)reader["id"];
				connection.Close();
				return (true, id);
			}

			connection.Close();
			return (false, 0);

		}


		public bool AddMedicalExaminationForm(int patientId ,MedicalExaminationForm medicalExaminationForm)
		{
			var connectionString = GetConnectionString();
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();

			DateTime currentDate = DateTime.Now;
            string formatDate = currentDate.ToString("yyyy-MM-dd");
			int id = UserSessionService.Instance.LoggedInUserId;


			var command = new SqlCommand("INSERT INTO MedicalExaminationForm (PatientId, StaffId, DoctorId, Time, Symptom) VALUES (@PatientId, @StaffId, @DoctorId, @Time, @Symptom)", connection);

			
		
			AddParameters(command,
				("@PatientId", patientId),
				("@StaffId", id),
				("@DoctorId", medicalExaminationForm.DoctorId),
                ("@Time", formatDate),
                ("@Symptom", medicalExaminationForm.Symptoms));
			
			int result = command.ExecuteNonQuery();

			connection.Close();
			return result > 0;
		}

		public List<Doctor> GetInforDoctor()
		{
			var connectionString = GetConnectionString();
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();

			string query1 = $"SELECT id, role FROM EndUser WHERE username = @Username AND password = @Password";
			var command1 = new SqlCommand(query1, connection);

			var query = $"""
							SELECT e.id, e.name as doctorName, s.id as specialtyId, s.name as specialtyName, d.room 
							FROM EndUser e JOIN Doctor d ON e.id = d.userId
								JOIN Specialty s ON d.specialtyId = s.id
							WHERE e.role = @Role;  
						""";
			var command = new SqlCommand(query, connection);
			AddParameters(command,
				("@Role", "doctor"));

			var reader = command.ExecuteReader();
			var result = new List<Doctor>();

			while (reader.Read())
			{
				var doctor = new Doctor
				{
					Id = (int)reader["id"],
					name = reader["doctorName"].ToString(),
					SpecialtyId = (int)reader["specialtyId"],
					SpecialtyName = reader["specialtyName"].ToString(),
					Room = reader["room"].ToString()
				};
				result.Add(doctor);
			}
			int count = result.Count;

			connection.Close();
			return result;
		}


		// Lấy phiếu khám bệnh
		// Thêm thông tin bệnh nhân
		public Tuple<List<MedicalExaminationForm>, int> GetMedicalExaminationForm(
			int page,
			int rowsPerPage, 
			string keyword, 
			Dictionary<string, SortType> sortOptions)
		{
			var result = new List<MedicalExaminationForm>();
			var connectionString = GetConnectionString();
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();

			string sortString = "ORDER BY ";
			bool useDefault = true;

			foreach (var item in sortOptions)
			{
				useDefault = false;
				if(item.Key == "patientId")
				{
					if (item.Value == SortType.Ascending)
					{
						sortString += "patientId ASC, ";
					}
					else
					{
						sortString += "patientId DESC, ";
					}
				}
			}

			if(useDefault)
			{
				sortString +="ID ";
			}

			var sql = $"""
				SELECT count(*) over() as Total, id, patientId, staffId, time, symptom, doctorId
				FROM MedicalExaminationForm
				WHERE patientId like @Keyword
				{sortString}
				OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
				""";

			var command = new SqlCommand(sql, connection);
			AddParameters(command, ("@Skip", (page - 1) * rowsPerPage), ("@Take", rowsPerPage), ("@Keyword", $"%{keyword}%"));
			var reader = command.ExecuteReader();
			int count = -1;

			while (reader.Read())
			{
				if (count == -1)
				{
					count = (int)reader["Total"];
				}

				var medicalExaminationForm = new MedicalExaminationForm();
				medicalExaminationForm.Id = (int)reader["id"];
				medicalExaminationForm.PatientId = (int)reader["patientId"];
				medicalExaminationForm.StaffId = (int)reader["staffId"];
				medicalExaminationForm.Time = (DateTime)reader["time"];
				medicalExaminationForm.Symptoms = (string)reader["symptom"];
				medicalExaminationForm.DoctorId = (int)reader["doctorId"];

				result.Add(medicalExaminationForm);
			}
			connection.Close();
			return new Tuple<List<MedicalExaminationForm>, int>(result, count);
		}
	
        public string DecryptionPassword(string encryptedPasswordInBase64, string entropyInBase64)
        {
            var encryptedPasswordInBytes = Convert.FromBase64String(encryptedPasswordInBase64);
            var entropyInBytes = Convert.FromBase64String(entropyInBase64);
            var passwordInBytes = ProtectedData.Unprotect(encryptedPasswordInBytes, entropyInBytes, DataProtectionScope.CurrentUser);
            var password = Encoding.UTF8.GetString(passwordInBytes);
            return password;
        }


        /*====================================================================================================*/

        public MedicalExaminationForm GetMedicalExaminationFormById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM MedicalExaminationForm WHERE id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new MedicalExaminationForm
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            PatientId = reader.GetInt32(reader.GetOrdinal("patientId")),
                            StaffId = reader.GetInt32(reader.GetOrdinal("staffId")),
                            Time = reader.GetDateTime(reader.GetOrdinal("time")),
                            Symptoms = reader.IsDBNull(reader.GetOrdinal("symptom")) ? (string)null : reader.GetString(reader.GetOrdinal("symptom")),
                            DoctorId = reader.GetInt32(reader.GetOrdinal("doctorId"))
                        };
                    }
                }
            }
            return null;
        }

        public MedicalRecord GetMedicalRecordByExaminationFormId(int medicalExaminationFormId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM MedicalRecord WHERE MedicalExaminationFormID = @ExaminationFormId", connection);
                command.Parameters.AddWithValue("@ExaminationFormId", medicalExaminationFormId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new MedicalRecord
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            DoctorId = reader.GetInt32(reader.GetOrdinal("doctorId")),
                            MedicalExaminationFormID = reader.GetInt32(reader.GetOrdinal("MedicalExaminationFormID")),
                            Time = reader.GetDateTime(reader.GetOrdinal("time")),
                            Diagnosis = reader.GetString(reader.GetOrdinal("diagnosis"))
                        };
                    }
                }
            }
            return null;
        }

        // New method to create a MedicalRecord using data from MedicalExaminationForm
        public MedicalRecord CreateMedicalRecordFromForm(MedicalExaminationForm form)
        {
            var record = new MedicalRecord
            {
                DoctorId = form.DoctorId,
                MedicalExaminationFormID = form.Id,
                Time = DateTime.Now,  // Can use form.Time if needed
                Diagnosis = string.Empty
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO MedicalRecord (doctorId, MedicalExaminationFormID, time, diagnosis) " +
                    "VALUES (@DoctorId, @MedicalExaminationFormID, @Time, @Diagnosis); SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@DoctorId", record.DoctorId);
                command.Parameters.AddWithValue("@MedicalExaminationFormID", record.MedicalExaminationFormID);
                command.Parameters.AddWithValue("@Time", record.Time);
                command.Parameters.AddWithValue("@Diagnosis", record.Diagnosis);

                record.Id = Convert.ToInt32(command.ExecuteScalar());  // Capture new ID
            }

            return record;
        }

        public void UpdateMedicalRecord(MedicalRecord record)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "UPDATE MedicalRecord SET doctorId = @DoctorId, time = @Time, diagnosis = @Diagnosis " +
                    "WHERE MedicalExaminationFormID = @MedicalExaminationFormID", connection);

                command.Parameters.AddWithValue("@DoctorId", record.DoctorId);
                command.Parameters.AddWithValue("@Time", record.Time);
                command.Parameters.AddWithValue("@Diagnosis", record.Diagnosis);
                command.Parameters.AddWithValue("@MedicalExaminationFormID", record.MedicalExaminationFormID);

                command.ExecuteNonQuery();
            }
        }

        /*====================================================================================================*/

        public void SavePrescription(Prescription prescription)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO Prescription (MedicalRecordId, MedicineId, Quantity) " +
                    "VALUES (@MedicalRecordId, @MedicineId, @Quantity)", connection);

                command.Parameters.AddWithValue("@MedicalRecordId", prescription.MedicalRecordId);
                //command.Parameters.AddWithValue("@MedicineId", prescription.MedicineId);
                //command.Parameters.AddWithValue("@Quantity", prescription.Quantity);

                command.ExecuteNonQuery();
            }
        }

        public void UpdateMedicineQuantity(int medicineId, int quantityChange)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "UPDATE Medicine SET Quantity = Quantity + @QuantityChange WHERE Id = @MedicineId AND Quantity >= 0";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@QuantityChange", quantityChange);
                command.Parameters.AddWithValue("@MedicineId", medicineId);
                command.ExecuteNonQuery();
            }
        }

        public List<Medicine> GetAvailableMedicines()
        {
            var medicines = new List<Medicine>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, Name, Quantity FROM Medicine WHERE Quantity > 0", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        medicines.Add(new Medicine
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Quantity = reader.GetInt32(2)
                        });
                    }
                }
            }

            return medicines;
        }

    }
}
