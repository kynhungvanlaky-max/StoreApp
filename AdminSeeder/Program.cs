using System;
using Microsoft.Data.SqlClient;
using BCrypt.Net;

namespace AdminSeeder
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=localhost;Database=GameStoreDB;User Id=hoangky12345s;Password=Hoangky123@;TrustServerCertificate=True;MultipleActiveResultSets=true;";
            
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Get Admin RoleId
                    int roleId = 1; // Default
                    using (SqlCommand cmdRole = new SqlCommand("SELECT Id FROM Roles WHERE RoleName = 'Administrator'", conn))
                    {
                        var result = cmdRole.ExecuteScalar();
                        if (result != null)
                        {
                            roleId = Convert.ToInt32(result);
                        }
                        else
                        {
                            Console.WriteLine("Role 'Administrator' not found. Creating it...");
                            using (SqlCommand insertRole = new SqlCommand("INSERT INTO Roles (RoleName) OUTPUT INSERTED.Id VALUES ('Administrator')", conn))
                            {
                                roleId = Convert.ToInt32(insertRole.ExecuteScalar());
                            }
                        }
                    }

                    // 2. Insert User
                    string username = "testadmin";
                    string email = "testadmin@example.com";
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @u", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@u", username);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            Console.WriteLine($"User {username} already exists!");
                            return;
                        }
                    }

                    string insertSql = "INSERT INTO Users (Username, Email, PasswordHash, WalletBalance, RoleId, CreatedAt, IsLocked) " +
                                       "VALUES (@u, @e, @p, 0, @r, GETDATE(), 0)";
                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.Parameters.AddWithValue("@p", passwordHash);
                        cmd.Parameters.AddWithValue("@r", roleId);

                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("Admin user 'testadmin' with password '123456' created successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
