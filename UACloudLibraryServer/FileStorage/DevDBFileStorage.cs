/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Cloud.Library.Interfaces;

    /// <summary>
    /// Database storage class: single store makes some development scenarios easier
	/// For example: database deletion/recreate leaves DB out of sync with file store)
	/// Multiple copies collide on file store
    /// </summary>
    public class DevDbFileStorage : IFileStorage
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DevDbFileStorage(ILoggerFactory logger, AppDbContext dbContext, IConfiguration configuration)
        {
            _logger = logger.CreateLogger("LocalFileStorage");
            _dbContext = dbContext;
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingFile = await _dbContext.FindAsync<DevDbFiles>(name).ConfigureAwait(false);
                if (existingFile != null)
                {
                    return name;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return the filename for storage in the index db
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingFile = await _dbContext.FindAsync<DevDbFiles>(name).ConfigureAwait(false);
                if (existingFile != null)
                {
                    existingFile.Blob = content;
                    _dbContext.Update(existingFile);
                }
                else
                {
                    DevDbFiles newFile = new DevDbFiles {
                        Name = name,
                        Blob = content,
                    };
                    _dbContext.Add(newFile);
                }
                await _dbContext.SaveChangesAsync(true).ConfigureAwait(false);
                return name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Download a blob to a file.
        /// </summary>
        public async Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingFile = await _dbContext.FindAsync<DevDbFiles>(name).ConfigureAwait(false);
                return existingFile?.Blob;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        public async Task DeleteFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingFile = await _dbContext.FindAsync<DevDbFiles>(name).ConfigureAwait(false);
                if (existingFile != null)
                {
                    _dbContext.Remove(existingFile);
                    await _dbContext.SaveChangesAsync(true).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DevDbFiles>();
        }
    }
    public class DevDbFiles
    {
        [Key]
        public string Name { get; set; }
        public string Blob { get; set; }
    }
}
