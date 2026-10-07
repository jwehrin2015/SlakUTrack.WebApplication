import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { buildAttendanceCsv, buildAttendanceReportFilename, countScheduledDates, formatLocalDate } from './utils/attendance.js'

const weekdays = [
  { value: 'Monday', short: 'M', label: 'Mon' },
  { value: 'Tuesday', short: 'T', label: 'Tue' },
  { value: 'Wednesday', short: 'W', label: 'Wed' },
  { value: 'Thursday', short: 'T', label: 'Thu' },
  { value: 'Friday', short: 'F', label: 'Fri' },
]

async function api(path, options) {
  const response = await fetch(`/api${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  })
  const body = response.status === 204 ? null : await response.json()
  if (!response.ok) {
    throw new Error(body?.error ?? body?.title ?? 'The request could not be completed.')
  }
  return body
}

function Icon({ name, size = 19 }) {
  const paths = {
    report: <><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" /><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2Z" /><path d="M8 7h8M8 11h8" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 11h18" /></>,
    settings: <><circle cx="12" cy="12" r="3" /><path d="m19.4 15 .1.1 1.4 1.1-1.4 2.4-1.7-.7a8 8 0 0 1-1.6.9l-.3 1.8h-2.8l-.3-1.8a8 8 0 0 1-1.6-.9l-1.7.7-1.4-2.4 1.4-1.1a7 7 0 0 1 0-1.9l-1.4-1.1 1.4-2.4 1.7.7a8 8 0 0 1 1.6-.9l.3-1.8h2.8l.3 1.8a8 8 0 0 1 1.6.9l1.7-.7 1.4 2.4-1.4 1.1a7 7 0 0 1 0 1.9Z" transform="translate(-1 -1)" /></>,
    download: <><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><path d="m7 10 5 5 5-5M12 15V3" /></>,
    print: <><path d="M6 9V2h12v7M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" /><path d="M6 14h12v8H6z" /></>,
    arrow: <><path d="M5 12h14M12 5l7 7-7 7" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    users: <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="10" cy="7" r="4" /><path d="M20 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></>,
  }
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>
}

function Field({ label, children, className = '' }) {
  return <label className={`field ${className}`}><span className="field-label">{label}</span>{children}</label>
}

function SelectField({ label, value, onChange, options, placeholder, disabled = false, className = '' }) {
  return (
    <Field label={label} className={className}>
      <select value={value} onChange={(event) => onChange(event.target.value)} disabled={disabled}>
        <option value="">{placeholder}</option>
        {options.map((option) => <option key={option.id} value={option.id}>{option.label}</option>)}
      </select>
    </Field>
  )
}

function App() {
  const [page, setPage] = useState('report')
  const [instructors, setInstructors] = useState([])
  const [locations, setLocations] = useState([])
  const [settings, setSettings] = useState(null)
  const [todayLabel, setTodayLabel] = useState('')
  const [status, setStatus] = useState({ message: '', error: false })
  const [loading, setLoading] = useState(true)

  const [reportInstructor, setReportInstructor] = useState('')
  const [reportCourses, setReportCourses] = useState([])
  const [reportCourse, setReportCourse] = useState('')
  const [classDates, setClassDates] = useState([])
  const [classDate, setClassDate] = useState('')
  const [report, setReport] = useState(null)
  const [reportBusy, setReportBusy] = useState(false)

  const [scheduleInstructor, setScheduleInstructor] = useState('')
  const [scheduleCourses, setScheduleCourses] = useState([])
  const [scheduleCourse, setScheduleCourse] = useState('')
  const [location, setLocation] = useState('')
  const [days, setDays] = useState([])
  const [startTime, setStartTime] = useState('09:00')
  const [endTime, setEndTime] = useState('10:00')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [scheduleBusy, setScheduleBusy] = useState(false)

  useEffect(() => {
    let active = true
    Promise.all([api('/instructors'), api('/locations'), api('/settings')])
      .then(([instructorOptions, roomOptions, appSettings]) => {
        if (!active) return
        setInstructors(instructorOptions)
        setLocations(roomOptions)
        setSettings(appSettings)
        const now = new Date()
        const date = formatLocalDate(now)
        setStartDate(date)
        setEndDate(date)
        setTodayLabel(new Intl.DateTimeFormat('en', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' }).format(now))
      })
      .catch((error) => {
        if (active) setStatus({ message: error.message, error: true })
      })
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [])

  useEffect(() => {
    let active = true
    if (!reportInstructor) return undefined
    api(`/instructors/${encodeURIComponent(reportInstructor)}/courses`)
      .then((result) => active && setReportCourses(result))
      .catch((error) => active && setStatus({ message: error.message, error: true }))
    return () => { active = false }
  }, [reportInstructor])

  useEffect(() => {
    let active = true
    if (!reportCourse) return undefined
    api(`/courses/${encodeURIComponent(reportCourse)}/classes`)
      .then((result) => active && setClassDates(result))
      .catch((error) => active && setStatus({ message: error.message, error: true }))
    return () => { active = false }
  }, [reportCourse])

  useEffect(() => {
    let active = true
    if (!scheduleInstructor) return undefined
    api(`/instructors/${encodeURIComponent(scheduleInstructor)}/courses`)
      .then((result) => active && setScheduleCourses(result))
      .catch((error) => active && setStatus({ message: error.message, error: true }))
    return () => { active = false }
  }, [scheduleInstructor])

  const scheduleCount = useMemo(() => {
    return countScheduledDates(startDate, endDate, days)
  }, [startDate, endDate, days])

  async function generateReport(event) {
    event.preventDefault()
    if (!reportCourse || !classDate) {
      setStatus({ message: 'Select a course and class date to generate a report.', error: true })
      return
    }
    setReportBusy(true)
    setStatus({ message: '', error: false })
    try {
      setReport(await api(`/reports?courseId=${encodeURIComponent(reportCourse)}&classId=${encodeURIComponent(classDate)}`))
      setStatus({ message: 'Attendance report is ready.', error: false })
    } catch (error) {
      setStatus({ message: error.message, error: true })
    } finally {
      setReportBusy(false)
    }
  }

  async function addClasses(event) {
    event.preventDefault()
    setScheduleBusy(true)
    setStatus({ message: '', error: false })
    try {
      const result = await api('/classes/recurring', {
        method: 'POST',
        body: JSON.stringify({
          instructorId: scheduleInstructor,
          locationId: location,
          courseId: scheduleCourse,
          startTime,
          endTime,
          startDate,
          endDate,
          daysOfWeek: days,
        }),
      })
      setStatus({ message: `Added ${result.count} class${result.count === 1 ? '' : 'es'} to the schedule.`, error: false })
      if (reportCourse === scheduleCourse) {
        const refreshedClasses = await api(`/courses/${encodeURIComponent(scheduleCourse)}/classes`)
        setClassDates(refreshedClasses)
      }
    } catch (error) {
      setStatus({ message: error.message, error: true })
    } finally {
      setScheduleBusy(false)
    }
  }

  function downloadCsv() {
    if (!report) return
    const url = URL.createObjectURL(new Blob([buildAttendanceCsv(report)], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a')
    link.href = url
    link.download = buildAttendanceReportFilename(new Date())
    link.click()
    window.setTimeout(() => URL.revokeObjectURL(url), 1000)
    setStatus({ message: 'CSV report downloaded.', error: false })
  }

  const presentCount = report?.rows.filter((row) => row.attended.toLowerCase() === 'yes').length ?? 0
  const attendanceRate = report?.rows.length ? Math.round((presentCount / report.rows.length) * 100) : 0
  const currentTitle = page === 'report' ? 'Attendance report' : page === 'schedule' ? 'Schedule classes' : 'Settings'

  function changeReportInstructor(value) {
    setReportInstructor(value)
    setReportCourses([])
    setReportCourse('')
    setClassDates([])
    setClassDate('')
    setReport(null)
  }

  function changeReportCourse(value) {
    setReportCourse(value)
    setClassDates([])
    setClassDate('')
    setReport(null)
  }

  function changeScheduleInstructor(value) {
    setScheduleInstructor(value)
    setScheduleCourses([])
    setScheduleCourse('')
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#" onClick={(event) => { event.preventDefault(); setPage('report') }}>
          <span className="brand-mark"><span /></span>
          <span className="brand-name">SLAK-U-Track<small>ATTENDANCE MANAGEMENT</small></span>
        </a>
        <div className="nav-caption">WORKSPACE</div>
        <nav className="side-nav" aria-label="Main navigation">
          <button className={page === 'report' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('report')}>
            <Icon name="report" /><span>Attendance report</span>
          </button>
          <button className={page === 'schedule' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('schedule')}>
            <Icon name="calendar" /><span>Schedule classes</span>
          </button>
          <button className={page === 'settings' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('settings')}>
            <Icon name="settings" /><span>Settings</span>
          </button>
        </nav>
        <div className="sidebar-bottom">
          <div className="connection-indicator"><span className={loading ? 'status-dot loading' : 'status-dot'} />{loading ? 'Connecting to database' : 'Local database connected'}</div>
          <div className="sidebar-version">SLAK-U-Track <span>WEB</span></div>
        </div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div className="breadcrumbs"><span>Workspace</span><span className="crumb-separator">/</span><strong>{currentTitle}</strong></div>
          <div className="topbar-context"><span className="topbar-dot" />Local workspace</div>
        </header>

        <div className="page-content">
          <section className="page-heading">
            <div>
              <div className="eyebrow">SLAK-U-TRACK <span>/</span> {page === 'report' ? 'REPORTING' : page === 'schedule' ? 'CLASS MANAGEMENT' : 'PREFERENCES'}</div>
              <h1>{page === 'report' ? 'Attendance report' : page === 'schedule' ? 'Schedule classes' : 'Settings'}</h1>
              <p>{page === 'report' ? 'Review and export student attendance for a class session.' : page === 'schedule' ? 'Create a recurring schedule for a course.' : 'View your local application and database information.'}</p>
            </div>
            {todayLabel && <div className="date-chip"><Icon name="calendar" size={16} /><span>{todayLabel}</span></div>}
          </section>

          {status.message && <div className={`notice ${status.error ? 'notice-error' : 'notice-success'}`} role={status.error ? 'alert' : 'status'}>
            <Icon name={status.error ? 'settings' : 'check'} size={17} /><span>{status.message}</span><button type="button" aria-label="Dismiss notification" onClick={() => setStatus({ message: '', error: false })}>×</button>
          </div>}

          {page === 'report' && (
            <div className="report-page">
              <section className="panel report-filter-panel">
                <div className="panel-heading">
                  <div className="panel-heading-icon"><Icon name="report" /></div>
                  <div><h2>Build a report</h2><p>Choose a professor, course, and class date to get started.</p></div>
                </div>
                <form className="report-form" onSubmit={generateReport}>
                  <SelectField label="Professor" value={reportInstructor} onChange={changeReportInstructor} options={instructors} placeholder="Select a professor" disabled={loading} />
                  <SelectField label="Course" value={reportCourse} onChange={changeReportCourse} options={reportCourses} placeholder="Select a course" disabled={!reportInstructor} />
                  <SelectField label="Class date" value={classDate} onChange={setClassDate} options={classDates.map((item) => ({ ...item, label: `Class ${item.id} — ${item.date}` }))} placeholder="Select a class date" disabled={!reportCourse} />
                  <button className="button button-primary generate-button" type="submit" disabled={reportBusy || !reportCourse || !classDate}>
                    {reportBusy ? <span className="spinner" /> : <Icon name="report" size={17} />}
                    {reportBusy ? 'Generating…' : 'Generate report'}
                  </button>
                </form>
              </section>

              {report ? (
                <>
                  <section className="report-summary">
                    <div className="summary-main">
                      <div className="summary-overline">CLASS SESSION</div>
                      <h2>{report.class.courseName}</h2>
                      <div className="summary-details">
                        <span><span className="detail-key">PROFESSOR</span>{report.class.professor}</span>
                        <span><span className="detail-key">COURSE ID</span>{report.class.courseId}</span>
                        <span><span className="detail-key">CLASS DATE</span>{report.class.date}</span>
                      </div>
                    </div>
                    <div className="summary-stats">
                      <div className="summary-stat"><span className="stat-icon"><Icon name="users" /></span><div><strong>{report.rows.length}</strong><small>ENROLLED</small></div></div>
                      <div className="summary-stat"><span className="stat-icon stat-icon-green"><Icon name="check" /></span><div><strong>{presentCount}</strong><small>ATTENDED</small></div></div>
                      <div className="summary-stat"><span className="stat-icon stat-icon-amber"><Icon name="report" /></span><div><strong>{attendanceRate}%</strong><small>ATTENDANCE</small></div></div>
                    </div>
                  </section>

                  <section className="panel table-panel">
                    <div className="table-heading">
                      <div><h2>Student attendance</h2><p>{report.rows.length} students registered in this course</p></div>
                      <div className="table-actions">
                        <button className="button button-secondary" type="button" onClick={downloadCsv}><Icon name="download" size={16} />Save CSV</button>
                        <button className="button button-primary" type="button" onClick={() => window.print()}><Icon name="print" size={16} />Print report</button>
                      </div>
                    </div>
                    <div className="table-scroll">
                      <table>
                        <thead><tr><th>STUDENT</th><th>ATTENDED</th><th>TIME IN</th><th>LAST DATE ATTENDED</th></tr></thead>
                        <tbody>
                          {report.rows.map((row, index) => (
                            <tr key={`${row.name}-${index}`}>
                              <td><span className="student-avatar">{row.name.split(/\s+/).map((part) => part[0]).join('').slice(0, 2).toUpperCase()}</span><span className="student-name">{row.name}</span></td>
                              <td>{row.attended ? <span className={`attendance-badge ${row.attended.toLowerCase() === 'yes' ? 'present' : 'absent'}`}><span />{row.attended}</span> : <span className="muted-dash">—</span>}</td>
                              <td>{row.timeIn ? <span className="time-value"><Icon name="clock" size={15} />{row.timeIn}</span> : <span className="muted-dash">—</span>}</td>
                              <td>{row.lastDateAttended || <span className="muted-dash">—</span>}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {report.rows.length === 0 && <div className="empty-table">No students are registered in this course.</div>}
                    </div>
                    <div className="table-footer"><span>Showing <strong>{report.rows.length}</strong> student{report.rows.length === 1 ? '' : 's'}</span><span>Sorted by name</span></div>
                  </section>
                </>
              ) : (
                <section className="empty-report">
                  <div className="empty-illustration"><Icon name="report" size={27} /></div>
                  <h2>Your report will appear here</h2>
                  <p>Select a professor, course, and class date above to view attendance details.</p>
                </section>
              )}
            </div>
          )}

          {page === 'schedule' && (
            <div className="schedule-layout">
              <section className="panel schedule-panel">
                <div className="panel-heading">
                  <div className="panel-heading-icon"><Icon name="calendar" /></div>
                  <div><h2>Recurring class details</h2><p>Classes will be added for each selected weekday in your date range.</p></div>
                </div>
                <form onSubmit={addClasses}>
                  <div className="schedule-grid">
                    <SelectField label="Professor" value={scheduleInstructor} onChange={changeScheduleInstructor} options={instructors} placeholder="Select a professor" disabled={loading} />
                    <SelectField label="Class room" value={location} onChange={setLocation} options={locations} placeholder="Select a class room" disabled={loading} />
                    <SelectField label="Course" value={scheduleCourse} onChange={setScheduleCourse} options={scheduleCourses} placeholder="Select a course" disabled={!scheduleInstructor} />
                    <Field label="Days of week" className="days-field">
                      <div className="weekday-picker">
                        {weekdays.map((day) => {
                          const selected = days.includes(day.value)
                          return <button key={day.value} type="button" className={selected ? 'weekday selected' : 'weekday'} aria-pressed={selected} onClick={() => setDays((current) => selected ? current.filter((item) => item !== day.value) : [...current, day.value])}>
                            <span className="weekday-short">{day.short}</span><span>{day.label}</span>
                          </button>
                        })}
                      </div>
                    </Field>
                    <Field label="Start time"><input type="time" value={startTime} onChange={(event) => setStartTime(event.target.value)} required /></Field>
                    <Field label="End time"><input type="time" value={endTime} onChange={(event) => setEndTime(event.target.value)} required /></Field>
                    <Field label="Start date"><input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} required /></Field>
                    <Field label="End date"><input type="date" value={endDate} min={startDate} onChange={(event) => setEndDate(event.target.value)} required /></Field>
                  </div>
                  <div className="schedule-submit">
                    <span>{scheduleCount > 0 ? <><strong>{scheduleCount}</strong> class date{scheduleCount === 1 ? '' : 's'} will be created</> : 'Choose weekdays and a date range to preview classes'}</span>
                    <button className="button button-primary" type="submit" disabled={scheduleBusy || loading || !scheduleInstructor || !scheduleCourse || !location || !scheduleCount || endTime <= startTime}>
                      {scheduleBusy ? <span className="spinner" /> : <Icon name="check" size={17} />}
                      {scheduleBusy ? 'Adding classes…' : 'Add classes'}
                    </button>
                  </div>
                </form>
              </section>
              <aside className="schedule-note">
                <div className="note-icon"><Icon name="calendar" /></div>
                <h3>How recurring schedules work</h3>
                <ul>
                  <li>Each matching weekday becomes a separate class session.</li>
                  <li>Start and end dates are both included in the schedule.</li>
                  <li>New sessions are available immediately in attendance reports.</li>
                </ul>
              </aside>
            </div>
          )}

          {page === 'settings' && (
            <section className="panel settings-panel">
              <div className="panel-heading">
                <div className="panel-heading-icon"><Icon name="settings" /></div>
                <div><h2>Local database</h2><p>Your application data is stored locally on this machine.</p></div>
              </div>
              <div className="settings-list">
                <div className="setting-row"><div><strong>Database provider</strong><p>The attendance database engine used by this application.</p></div><span className="setting-value"><span className="sqlite-icon">S</span>{settings?.provider ?? 'SQLite'}</span></div>
                <div className="setting-row"><div><strong>Database file</strong><p>Back up this file to preserve your attendance and class schedule data.</p></div><code className="database-path">{settings?.databasePath ?? 'Loading database location…'}</code></div>
                <div className="setting-row"><div><strong>Storage</strong><p>Data is kept on this server and is not sent to a third-party service.</p></div><span className="local-pill"><span className="status-dot" />Local only</span></div>
              </div>
            </section>
          )}
          <footer className="page-footer">SLAK-U-Track <span>·</span> Attendance management</footer>
        </div>
      </main>
    </div>
  )
}

export default App
