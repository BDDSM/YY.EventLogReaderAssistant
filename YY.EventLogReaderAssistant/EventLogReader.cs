﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YY.EventLogReaderAssistant.Services;
using YY.EventLogReaderAssistant.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    public abstract partial class EventLogReader : IEventLogReader, IDisposable
    {
        #region Static Methods

        public static EventLogReader CreateReader(string pathLogFile)
        {
            FileAttributes attr = File.GetAttributes(pathLogFile);

            FileInfo logFileInfo = null;
            string logFileWithReferences;
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string currentLogFilesPath = pathLogFile;
                logFileWithReferences = string.Format("{0}{1}{2}", currentLogFilesPath, Path.DirectorySeparatorChar, @"1Cv8.lgf");
            }
            else
            {
                logFileInfo = new FileInfo(pathLogFile);
                logFileWithReferences = logFileInfo.FullName;
            }
                        
            if (!File.Exists(logFileWithReferences))
                logFileWithReferences = string.Format("{0}{1}{2}", pathLogFile, Path.DirectorySeparatorChar, @"1Cv8.lgd");

            if (File.Exists(logFileWithReferences))
            {
                if (logFileInfo == null) logFileInfo = new FileInfo(logFileWithReferences);

                string logFileExtension = logFileInfo.Extension.ToUpper();
                if (logFileExtension.EndsWith("LGF"))
                    return new EventLogLGFReader(logFileInfo.FullName);
                else if (logFileExtension.EndsWith("LGD"))
                    return new EventLogLGDReader(logFileInfo.FullName);
            }

            throw new ArgumentException("Invalid log file path");
        }

        #endregion

        #region Private Member Variables

        protected string _logFilePath;
        protected string _logFileDirectoryPath;
        protected long _currentFileEventNumber;
        public long CurrentFileEventNumber { get { return _currentFileEventNumber; } }
        public string LogFilePath { get { return _logFilePath; } }
        public string LogFileDirectoryPath { get { return _logFileDirectoryPath; } }

        protected List<Applications> _applications;
        protected List<Computers> _computers;
        protected List<Metadata> _metadata;
        protected List<Events> _events;
        protected List<PrimaryPorts> _primaryPorts;
        protected List<SecondaryPorts> _secondaryPorts;
        protected List<Users> _users;
        protected List<WorkServers> _workServers;
        protected RowData _currentRow;

        #endregion

        #region Constructor

        internal EventLogReader() : base() { }
        internal EventLogReader(string logFilePath)
        {
            _logFilePath = logFilePath;
            _logFileDirectoryPath = new FileInfo(_logFilePath).Directory.FullName;

            _applications = new List<Applications>();
            _computers = new List<Computers>();
            _metadata = new List<Metadata>();
            _events = new List<Events>();
            _primaryPorts = new List<PrimaryPorts>();
            _secondaryPorts = new List<SecondaryPorts>();
            _users = new List<Users>();
            _workServers = new List<WorkServers>();

            ReadEventLogReferences();
        }

        #endregion

        #region Public Properties

        public IReadOnlyList<Applications> Applications { get { return _applications; } }
        public IReadOnlyList<Computers> Computers { get { return _computers; } }
        public IReadOnlyList<Metadata> Metadata { get { return _metadata; } }
        public IReadOnlyList<Events> Events { get { return _events; } }
        public IReadOnlyList<PrimaryPorts> PrimaryPorts { get { return _primaryPorts; } }
        public IReadOnlyList<SecondaryPorts> SecondaryPorts { get { return _secondaryPorts; } }
        public IReadOnlyList<Users> Users { get { return _users; } }
        public IReadOnlyList<WorkServers> WorkServers { get { return _workServers; } }
        public RowData CurrentRow { get { return _currentRow; } }

        #endregion

        #region Public Methods

        public virtual bool Read()
        {
            throw new NotImplementedException();
        }
        public virtual bool GoToEvent(long eventNumber)
        {
            throw new NotImplementedException();
        }
        public virtual EventLogPosition GetCurrentPosition()
        {
            throw new NotImplementedException();
        }
        public virtual void SetCurrentPosition(EventLogPosition newPosition)
        {
            throw new NotImplementedException();
        }
        public virtual long Count()
        {
            throw new NotImplementedException();
        }
        public virtual void Reset()
        {
            throw new NotImplementedException();
        }
        public virtual void NextFile()
        {
            throw new NotImplementedException();
        }
        public virtual void Dispose()
        {
            _applications.Clear();
            _computers.Clear();
            _metadata.Clear();
            _events.Clear();
            _primaryPorts.Clear();
            _secondaryPorts.Clear();
            _users.Clear();
            _workServers.Clear();
            _currentRow = null;
        }

        public Users GetUserByCode(string code)
        {
            return GetUserByCode(code.ToInt64());
        }
        public Users GetUserByCode(long code)
        {
            return _users.Where(i => i.Code == code).FirstOrDefault();
        }

        public Computers GetComputerByCode(string code)
        {
            return GetComputerByCode(code.ToInt64());
        }
        public Computers GetComputerByCode(long code)
        {
            return _computers.Where(i => i.Code == code).FirstOrDefault();
        }

        public Applications GetApplicationByCode(string code)
        {
            return GetApplicationByCode(code.ToInt64());
        }
        public Applications GetApplicationByCode(long code)
        {
            return _applications.Where(i => i.Code == code).FirstOrDefault();
        }

        public Events GetEventByCode(string code)
        {
            return GetEventByCode(code.ToInt64());
        }
        public Events GetEventByCode(long code)
        {
            return _events.Where(i => i.Code == code).FirstOrDefault();
        }

        public Severity GetSeverityByCode(string code)
        {
            Severity severity;

            switch (code.Trim())
            {
                case "I":
                    severity = Severity.Information;
                    break;
                case "W":
                    severity = Severity.Warning;
                    break;
                case "E":
                    severity = Severity.Error;
                    break;
                case "N":
                    severity = Severity.Note;
                    break;
                default:
                    severity = Severity.Unknown;
                    break;
            }

            return severity;
        }
        public Severity GetSeverityByCode(long code)
        {
            try
            {
                return (Severity)code;
            } catch
            {
                return Severity.Unknown;
            }
        }

        public TransactionStatus GetTransactionStatus(string code)
        {
            TransactionStatus transactionStatus;

            if (code == "R")
                transactionStatus = TransactionStatus.Unfinished;
            else if (code == "N")
                transactionStatus = TransactionStatus.NotApplicable;
            else if (code == "U")
                transactionStatus = TransactionStatus.Committed;
            else if (code == "C")
                transactionStatus = TransactionStatus.RolledBack;
            else
                transactionStatus = TransactionStatus.Unknown;

            return transactionStatus;
        }
        public TransactionStatus GetTransactionStatus(long code)
        {
            try
            {
                return (TransactionStatus)code;
            } catch
            {
                return TransactionStatus.Unknown;
            }
        }

        public Metadata GetMetadataByCode(string code)
        {
            return GetMetadataByCode(code.ToInt64());
        }
        public Metadata GetMetadataByCode(long code)
        {
            return _metadata.Where(i => i.Code == code).FirstOrDefault();
        }

        public WorkServers GetWorkServerByCode(string code)
        {
            return GetWorkServerByCode(code.ToInt64());
        }
        public WorkServers GetWorkServerByCode(long code)
        {
            return _workServers.Where(i => i.Code == code).FirstOrDefault();
        }

        public PrimaryPorts GetPrimaryPortByCode(string code)
        {
            return GetPrimaryPortByCode(code.ToInt64());
        }
        public PrimaryPorts GetPrimaryPortByCode(long code)
        {
            return _primaryPorts.Where(i => i.Code == code).FirstOrDefault();
        }

        public SecondaryPorts GetSecondaryPortByCode(string code)
        {
            return GetSecondaryPortByCode(code.ToInt64());
        }
        public SecondaryPorts GetSecondaryPortByCode(long code)
        {
            return _secondaryPorts.Where(i => i.Code == code).FirstOrDefault();
        }

        #endregion

        #region Private Methods

        protected virtual void ReadEventLogReferences() { }

        #endregion

        #region Events

        public delegate void BeforeReadFileHandler(EventLogReader sender, BeforeReadFileEventArgs args);
        public delegate void AfterReadFileHandler(EventLogReader sender, AfterReadFileEventArgs args);
        public delegate void BeforeReadEventHandler(EventLogReader sender, BeforeReadEventArgs args);
        public delegate void AfterReadEventHandler(EventLogReader sender, AfterReadEventArgs args);
        public delegate void OnErrorEventHandler(EventLogReader sender, OnErrorEventArgs args);

        public event BeforeReadFileHandler BeforeReadFile;
        public event AfterReadFileHandler AfterReadFile;
        public event BeforeReadEventHandler BeforeReadEvent;
        public event AfterReadEventHandler AfterReadEvent;
        public event OnErrorEventHandler OnErrorEvent;

        protected void RaiseBeforeReadFile(BeforeReadFileEventArgs args)
        {
            BeforeReadFile?.Invoke(this, args);
        }
        protected void RaiseAfterReadFile(AfterReadFileEventArgs args)
        {
            AfterReadFile?.Invoke(this, args);
        }
        protected void RaiseBeforeRead(BeforeReadEventArgs args)
        {
            BeforeReadEvent?.Invoke(this, args);
        }
        protected void RaiseAfterRead(AfterReadEventArgs args)
        {
            AfterReadEvent?.Invoke(this, args);
        }
        protected void RaiseOnError(OnErrorEventArgs args)
        {
            OnErrorEvent?.Invoke(this, args);
        }

        #endregion
    }
}
