﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace nl.fhict.IntelliCloud.Common.DataTransfer
{
    /// <summary>
    /// A class representing the answer to a <see cref="Question"/>.
    /// </summary>
    [DataContract]
    public class Answer
    {
        /// <summary>
        /// Gets or sets the unique identifier of the answer.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the content of the answer. The content contains the answer given to the question.
        /// </summary>
        [DataMember]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the language the answer is written in.
        /// </summary>
        [DataMember]
        public LanguageDefinition LanguageDefinition { get; set; }
        
        /// <summary>
        /// Gets or sets the user that gave the answer.
        /// </summary>
        [DataMember]
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the state of the answer.
        /// </summary>
        [DataMember]
        public AnswerState AnswerState { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time of the answer.
        /// </summary>
        [DataMember]
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the keywords that are linked to the answer.
        /// </summary>
        [DataMember]
        public IList<Keyword> Keywords { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the answer is private. When a answer is private it can only be 
        /// viewed by users of type <see cref="UserType.Employee"/>.
        /// </summary>
        [DataMember]
        public bool IsPrivate { get; set; }
    }
}