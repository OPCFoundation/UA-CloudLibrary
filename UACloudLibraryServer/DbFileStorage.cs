/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library
{
    [Table("DbFiles")]
    public class DbFiles
    {
        [Key]
        public string Name { get; set; }

        public string Blob { get; set; }

        public string Values { get; set; }
    }

    /// <summary>
    /// Database storage class: single store makes some development scenarios easier
	/// For example: database deletion/recreate leaves DB out of sync with file store)
	/// Multiple copies collide on file store
    /// </summary>
    public class DbFileStorage
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DbFileStorage(ILoggerFactory logger, AppDbContext dbContext)
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
                DbFiles existingFile = await _dbContext.DBFiles.FindAsync([name], cancellationToken).ConfigureAwait(false);
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
        public async Task<string> UploadFileAsync(string name, string nodesetXml, string values, CancellationToken cancellationToken = default)
        {
            try
            {
                DbFiles existingFile = await _dbContext.DBFiles.Where(n => n.Name == name).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                string cleanXml = new string(nodesetXml.Where(c => c != '\0').ToArray());
                if (existingFile != null)
                {
                    existingFile.Blob = cleanXml;
                    existingFile.Values = values;

                    _dbContext.Update(existingFile);
                }
                else
                {
                    DbFiles newFile = new DbFiles {
                        Name = name,
                        Blob = cleanXml,
                        Values = values
                    };

                    _dbContext.Add(newFile);
                }

                await _dbContext.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);

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
        public async Task<DbFiles> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.DBFiles.AsNoTracking().Where(n => n.Name == name).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
                DbFiles existingFile = await _dbContext.DBFiles.Where(n => n.Name == name).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (existingFile != null)
                {
                    _dbContext.Remove(existingFile);

                    await _dbContext.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }
}
