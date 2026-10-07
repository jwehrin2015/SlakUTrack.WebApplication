import assert from 'node:assert/strict'
import test from 'node:test'
import {
  buildAttendanceCsv,
  buildAttendanceReportFilename,
  countScheduledDates,
  formatLocalDate,
} from './attendance.js'

test('formats a date using the local calendar date', () => {
  assert.equal(formatLocalDate(new Date(2026, 0, 5, 23, 30)), '2026-01-05')
})

test('counts matching weekdays inclusively at both ends of a range', () => {
  assert.equal(countScheduledDates('2026-10-05', '2026-10-16', ['Monday', 'Wednesday']), 4)
})

test('returns no scheduled dates for an empty selection, reversed range, or invalid date', () => {
  assert.equal(countScheduledDates('2026-10-05', '2026-10-09', []), 0)
  assert.equal(countScheduledDates('2026-10-09', '2026-10-05', ['Monday']), 0)
  assert.equal(countScheduledDates('not-a-date', '2026-10-09', ['Monday']), 0)
})

test('formats attendance CSV metadata, headers, and escaped values', () => {
  const csv = buildAttendanceCsv({
    class: {
      courseId: 'CS-101',
      professor: 'Ada Lovelace',
      courseName: 'Research, "Methods"',
      date: '2026-10-05',
    },
    rows: [
      { name: 'Grace, Hopper', attended: 'Yes', timeIn: '09:01:00', lastDateAttended: '2026-10-05' },
      { name: 'Alan Turing', attended: 'No', timeIn: '', lastDateAttended: '' },
    ],
  })

  assert.equal(csv, '\uFEFFCourse ID:,CS-101\r\nProfessor:,Ada Lovelace\r\nCourse:,"Research, ""Methods"""\r\nDate:,2026-10-05\r\nName,Attended,TimeIn,LastDateAttended\r\n"Grace, Hopper",Yes,09:01:00,2026-10-05\r\nAlan Turing,No,,\r\n')
})

test('builds the report filename in local time', () => {
  assert.equal(buildAttendanceReportFilename(new Date(2026, 9, 7, 9, 8, 6)), 'SLAK-U-Track_Report_20261007_090806.csv')
})
