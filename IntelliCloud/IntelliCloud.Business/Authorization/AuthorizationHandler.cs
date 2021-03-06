﻿using nl.fhict.IntelliCloud.Common.CustomException;
using nl.fhict.IntelliCloud.Common.DataTransfer;
using nl.fhict.IntelliCloud.Data.Context;
using nl.fhict.IntelliCloud.Data.Model;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace nl.fhict.IntelliCloud.Business.Authorization
{
    /// <summary>
    /// Class used for user authorization.
    /// </summary>
    public class AuthorizationHandler
    {
        /// <summary>
        /// Property that contains a User object representing the authorized user.
        /// </summary>
        public static User AuthorizedUser { get; set; }

        /// <summary>
        /// Field that contains an instance of class ConvertEntities, used for converting entities.
        /// </summary>
        private ConvertEntities convertEntities;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthorizationHandler()
        {
            this.convertEntities = new ConvertEntities();
        }

        /// <summary>
        /// Method for parsing the Base64 representation of the JSON object to an instance of AuthorizationToken
        /// </summary>
        /// <param name="authenticationToken">Base64 encoded string of the JSON object (value of the AuthorizationToken HTTP header).<param>
        /// <returns>Instance of class AuthorizationToken.</returns>
        private AuthorizationToken ParseToken(string authorizationToken)
        {
            // Decode the Base64 representation of the JSON object
            byte[] tokenBytes = Convert.FromBase64String(authorizationToken);

            using (MemoryStream stream = new MemoryStream(tokenBytes))
            {
                // Initialize serializer (used for deserializing the JSON representation of the AuthorizationToken)
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(AuthorizationToken));
                AuthorizationToken parsedToken = (AuthorizationToken)jsonSerializer.ReadObject(stream);

                return parsedToken;
            }
        }

        /// <summary>
        /// Method for retrieving available user information from the Access Token issuer.
        /// </summary>
        /// <param name="token">Instance of class AuthorizationToken.<param>
        /// <returns>Instance of class OpenIDUserInfo.</returns>
        private OpenIDUserInfo RetrieveUserInfo(AuthorizationToken token)
        {
            // String that will contain the endpoint URL of the issuer specified in the token
            string userInfoEndpointUrl = "";

            // Get the endpoint URL of the issuer from the context
            using (IntelliCloudContext context = new IntelliCloudContext())
            {
                SourceDefinitionEntity sourceDefinition = context.SourceDefinitions.SingleOrDefault(sd => sd.Name.Equals(token.Issuer));

                if (sourceDefinition == null)
                    throw new NotFoundException("No source definition entity exists for the specified issuer.");

                userInfoEndpointUrl = sourceDefinition.Url;
            }

            // Get available user information from the Access Token issuer.
            string requestUrl = String.Format(userInfoEndpointUrl, token.AccessToken);
            WebRequest request = WebRequest.Create(requestUrl);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string userInfo = reader.ReadToEnd();
            reader.Close();
            response.Close();

            // Convert the user info string to a byte array for further processing
            byte[] userInfoBytes = Encoding.UTF8.GetBytes(userInfo);

            using (MemoryStream stream = new MemoryStream(userInfoBytes))
            {
                // Initialize serializer (used for deserializing the JSON representation of the AuthorizationToken)
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(OpenIDUserInfo));
                OpenIDUserInfo parsedUserInfo = (OpenIDUserInfo)jsonSerializer.ReadObject(stream);
                parsedUserInfo.Issuer = token.Issuer;

                return parsedUserInfo;
            }
        }

        /// <summary>
        /// Method for retrieving user info based on the authorization token in the AuthorizationToken HTTP header.
        /// </summary>
        /// <param name="authorizationToken">The authorization token that will be used to retrieve user info.</param>
        /// <param name="userInfo">Reference to an object of class OpenIDUserInfo - will be set to an instance of class OpenIDUserInfo on success or null if no user info could be retrieved.</param>
        /// <returns>Boolean value indicating if user info could be retrieved.</returns>
        public bool TryRetrieveUserInfo(string authorizationToken, out OpenIDUserInfo userInfo)
        {
            // OpenIDUserInfo object that will contain an instance containing all data received from the Access Token issuer.
            userInfo = null;
            
            try
            {
                // Parse the authorization token and retrieve user info from the Access Token issuer.
                AuthorizationToken parsedToken = this.ParseToken(authorizationToken);
                userInfo = this.RetrieveUserInfo(parsedToken);
            }
            catch
            {
                // Ignore all exceptions, return null since no user info could be retrieved
            }

            // Return true or false indicating if a user could be matched
            return (userInfo != null) ? true : false;
        }

        /// <summary>
        /// Method for matching a user based on an instance of class OpenIDUserInfo.
        /// </summary>
        /// <param name="userInfo">The instance of class OpenIDUserInfo that will be used to match a user.</param>
        /// <param name="matchedUser">Reference to an object of class User - will be set to an instance of class User on success or null if no user could be matched.</param>
        /// <returns>Boolean value indicating if a user could be matched.</returns>
        public bool TryMatchUser(OpenIDUserInfo userInfo, out User matchedUser)
        {
            // User object that will contain the matched User object on success
            matchedUser = null;

            // Only attempt to match a user when an instance of class OpenIDUserInfo has been supplied
            if (userInfo != null)
            {
                using (IntelliCloudContext context = new IntelliCloudContext())
                {
                    // Get the user entity from the context
                    UserEntity userEntity = context.Users
                                            .Include(u => u.Sources.Select(s => s.SourceDefinition))
                                            .SingleOrDefault(s => s.Sources.Any(a => (a.SourceDefinition.Name == "Mail" && a.Value == userInfo.Email) || (a.SourceDefinition.Name == userInfo.Issuer && a.Value == userInfo.Sub)));

                    // Only continue if the user entity was found
                    if (userEntity != null)
                    {
                        // Update the user's first name and last name
                        userEntity.FirstName = userInfo.GivenName;
                        userEntity.LastName = userInfo.FamilyName;

                        // Update the user's id from the issuer
                        userEntity.Sources.Where(s => s.SourceDefinition.Name == userInfo.Issuer)
                                          .Select(s => { s.Value = userInfo.Sub; return s; });

                        // Save the changes to the context
                        context.SaveChanges();

                        // Convert the UserEntity to an instance of class User and set in the reference
                        matchedUser = this.convertEntities.UserEntityToUser(userEntity);
                    }
                }
            }

            // Return true or false indicating if a user could be matched
            return (matchedUser != null) ? true : false;
        }

        /// <summary>
        /// Method for creating a new user based on an instance of class OpenIDUserInfo.
        /// </summary>
        /// <param name="userInfo">The instance of class OpenIDUserInfo that will be used to create the new user.</param>
        /// <param name="createdUser">Reference to an object of class User - will be set to an instance of class User on success or null if the user could not be created.</param>
        /// <returns>Boolean value indicating if the user could be created.</returns>
        public bool TryCreateNewUser(OpenIDUserInfo userInfo, out User createdUser)
        {
            // User object that will contain the newly created User object on success
            createdUser = null;

            // Only attempt to create a new user when an instance of class OpenIDUserInfo has been supplied
            if (userInfo != null)
            {
                // No user could be matched; create a new user based on the retrieved user info
                UserEntity userEntity = null;
                using (IntelliCloudContext context = new IntelliCloudContext())
                {
                    // Create a new user based on the retrieved user info
                    userEntity = new UserEntity()
                    {
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        Type = UserType.Customer,
                        CreationTime = DateTime.UtcNow
                    };
                    context.Users.Add(userEntity);

                    // Create a new source for the user's email address
                    SourceDefinitionEntity mailSourceDefinition = context.SourceDefinitions.SingleOrDefault(sd => sd.Name.Equals("Mail"));
                    SourceEntity mailSourceEntity = new SourceEntity()
                    {
                        Value = userInfo.Email,
                        CreationTime = DateTime.UtcNow,
                        SourceDefinition = mailSourceDefinition,
                        User = userEntity,
                    };
                    context.Sources.Add(mailSourceEntity);

                    // Create a new source for the user's id from the issuer
                    SourceDefinitionEntity issuerSourceDefinition = context.SourceDefinitions.SingleOrDefault(sd => sd.Name.Equals(userInfo.Issuer));
                    SourceEntity issuerSourceEntity = new SourceEntity()
                    {
                        Value = userInfo.Sub,
                        CreationTime = DateTime.UtcNow,
                        SourceDefinition = issuerSourceDefinition,
                        User = userEntity,
                    };
                    context.Sources.Add(issuerSourceEntity);

                    // Save the changes to the context
                    context.SaveChanges();
                }

                // Convert the UserEntity instance to an instance of class User and set it in the matchedUser variable
                createdUser = this.convertEntities.UserEntityToUser(userEntity);
            }

            // Return true or false indicating if the user has been created
            return (createdUser != null) ? true : false;
        }
    }
}