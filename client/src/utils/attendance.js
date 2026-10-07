export function formatLocalDate(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function countScheduledDates(startDate, endDate, daysOfWeek) {
  if (!startDate || !endDate || !daysOfWeek.length) return 0

  const start = new Date(`${startDate}T00:00:00Z`)
  const end = new Date(`${endDate}T00:00:00Z`)
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end < start) return 0

  let count = 0
  const selectedDays = new Set(daysOfWeek)
  for (const date = new Date(start); date <= end; date.setUTCDate(date.getUTCDate() + 1)) {
    const day = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][date.getUTCDay()]
    if (selectedDays.has(day)) count += 1
  }
  return count
}

export function buildAttendanceCsv(report) {
  const csvRows = [
    ['Course ID:', report.class.courseId],
    ['Professor:', report.class.professor],
    ['Course:', report.class.courseName],
    ['Date:', report.class.date],
    ['Name', 'Attended', 'TimeIn', 'LastDateAttended'],
    ...report.rows.map((row) => [row.name, row.attended, row.timeIn, row.lastDateAttended]),
  ]
  const escape = (value) => {
    const text = String(value ?? '')
    return /[,"\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
  }
  return `\uFEFF${csvRows.map((row) => row.map(escape).join(',')).join('\r\n')}\r\n`
}

export function buildAttendanceReportFilename(date) {
  const timestamp = `${formatLocalDate(date).replaceAll('-', '')}_${String(date.getHours()).padStart(2, '0')}${String(date.getMinutes()).padStart(2, '0')}${String(date.getSeconds()).padStart(2, '0')}`
  return `SLAK-U-Track_Report_${timestamp}.csv`
}
