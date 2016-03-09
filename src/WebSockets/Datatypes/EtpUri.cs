﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Energistics.Datatypes
{
    /// <summary>
    /// Represents a URI supported by the Energistics Transfer Protocol (ETP).
    /// </summary>
    public struct EtpUri
    {
        private static readonly Regex _pattern = new Regex(@"^eml:\/\/((witsml|resqml|prodml|energyml)([0-9]+))(\/((obj_)?(\w+))(\(([\-\w]+)\))?)*?$", RegexOptions.IgnoreCase);
        private readonly Match _match;

        /// <summary>
        /// The root URI supported by the Discovery protocol.
        /// </summary>
        public const string RootUri = "/";

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpUri"/> struct.
        /// </summary>
        /// <param name="uri">The URI string.</param>
        public EtpUri(string uri)
        {
            _match = _pattern.Match(uri);

            Uri = uri;
            IsValid = _match.Success;

            Family = GetValue(_match, 2);
            Version = FormatVersion(GetValue(_match, 3));
            ContentType = new EtpContentType(Family, Version);
            ObjectType = null;
            ObjectId = null;

            if (HasRepeatValues(_match))
            {
                var last = GetObjectIds().Last();
                ObjectType = last.Key;
                ObjectId = last.Value;
                ContentType = new EtpContentType(Family, Version, ObjectType);
            }
        }

        /// <summary>
        /// Gets the original URI string.
        /// </summary>
        /// <value>The URI.</value>
        public string Uri { get; private set; }

        /// <summary>
        /// Gets the ML family name.
        /// </summary>
        /// <value>The ML family.</value>
        public string Family { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <value>The object identifier.</value>
        public string ObjectId { get; private set; }

        /// <summary>
        /// Gets the content type.
        /// </summary>
        /// <value>The type of the content.</value>
        public EtpContentType ContentType { get; private set; }

        /// <summary>
        /// Returns true if a valid URI was specified.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a base URI.
        /// </summary>
        /// <value><c>true</c> if this instance is a base URI; otherwise, <c>false</c>.</value>
        public bool IsBaseUri
        {
            get
            {
                return string.IsNullOrWhiteSpace(ObjectType)
                    && string.IsNullOrWhiteSpace(ObjectId);
            }
        }

        /// <summary>
        /// Determines whether this instance is related to the specified <see cref="EtpUri"/>.
        /// </summary>
        /// <param name="other">The other URI.</param>
        /// <returns>
        ///   <c>true</c> if the two <see cref="EtpUri"/> instances share the same family and
        ///   version; otherwise, <c>false</c>.
        /// </returns>
        public bool IsRelatedTo(EtpUri other)
        {
            return string.Equals(Family, other.Family, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(Version, other.Version, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets a collection of object type and ID key/value pairs.
        /// </summary>
        /// <returns>A collection of key/value pairs.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetObjectIds()
        {
            if (HasRepeatValues(_match))
            {
                var typeGroup = _match.Groups[7];
                var idGroup = _match.Groups[9];

                for (int i=0; i<typeGroup.Captures.Count; i++)
                {
                    var type = typeGroup.Captures[i].Value;
                    var id = idGroup.Captures.Count > i ? idGroup.Captures[i].Value : null;

                    yield return new KeyValuePair<string, string>(type, id);
                }
            }
        }

        public EtpUri Append(string objectType, string objectId = null)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return new EtpUri(Uri + "/" + objectType);

            return new EtpUri(Uri + string.Format("/{0}({1})", objectType, objectId));
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Uri;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="EtpUri"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(EtpUri uri)
        {
            return uri.ToString();
        }

        /// <summary>
        /// Determines whether the specified URI is a root URI.
        /// </summary>
        /// <param name="uri">The URI string.</param>
        /// <returns><c>true</c> if the URI is a root URI; otherwise, <c>false</c>.</returns>
        public static bool IsRoot(string uri)
        {
            return RootUri.Equals(uri);
        }

        /// <summary>
        /// Gets the value contained within the specified match at the specified index.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <param name="index">The index.</param>
        /// <returns>The matched value found at the specified index.</returns>
        private static string GetValue(Match match, int index)
        {
            return match.Success && match.Groups.Count > index
                ? match.Groups[index].Value
                : null;
        }

        /// <summary>
        /// Determines whether the specified match contains repeating values.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns><c>true</c> if any repeating groups were matched; otherwise, <c>false</c>.</returns>
        private static bool HasRepeatValues(Match match)
        {
            return match.Success && match.Groups[7].Captures.Count > 0;
        }

        /// <summary>
        /// Formats the version number.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>A dot delimited version number.</returns>
        private static string FormatVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return null;

            return string.Join(".", version.Trim().Select(x => x));
        }
    }
}