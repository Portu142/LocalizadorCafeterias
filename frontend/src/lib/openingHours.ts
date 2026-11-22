/**
 * Intérprete sencillo de horarios para el filtro «Abierto ahora».
 *
 * Entiende el formato `opening_hours` de OpenStreetMap más habitual
 * («Mo-Fr 08:00-20:00; Sa 09:00-14:00; Su off», «24/7», varios tramos con
 * coma, horarios que pasan de medianoche) y el formato en español de los
 * datos de ejemplo («Lun-Vie 08:00-20:00 · Sáb-Dom 09:00-21:00»).
 *
 * Ante cualquier cosa que no entienda (meses, festivos con fecha, semanas…)
 * devuelve `null`: es preferible «horario desconocido» a decir que un local
 * está abierto cuando no lo está.
 */

type Interval = { start: number; end: number };
type Schedule = Interval[][]; // índice 0 = lunes … 6 = domingo

const DAY_INDEX: Record<string, number> = {
  mo: 0, tu: 1, we: 2, th: 3, fr: 4, sa: 5, su: 6,
  lun: 0, mar: 1, mie: 2, jue: 3, vie: 4, sab: 5, dom: 6,
};

const ALL_DAYS = [0, 1, 2, 3, 4, 5, 6];

function normalize(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim();
}

function dayIndex(token: string): number | null {
  return DAY_INDEX[normalize(token)] ?? null;
}

/** «Mo-Fr,Su» → [0,1,2,3,4,6]. Admite rangos que dan la vuelta (Sa-Mo). */
function parseDays(value: string): number[] | null {
  const days = new Set<number>();

  for (const part of value.split(',')) {
    const [from, to] = part.split('-').map((token) => token.trim());
    const start = dayIndex(from ?? '');
    if (start === null) {
      return null;
    }

    if (to === undefined) {
      days.add(start);
      continue;
    }

    const end = dayIndex(to);
    if (end === null) {
      return null;
    }

    for (let day = start; ; day = (day + 1) % 7) {
      days.add(day);
      if (day === end) {
        break;
      }
    }
  }

  return [...days];
}

function toMinutes(value: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(value.trim());
  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  return hours <= 24 && minutes < 60 ? hours * 60 + minutes : null;
}

/** «08:00-14:00,17:00-20:00» → tramos en minutos. */
function parseTimes(value: string): Interval[] | null {
  const intervals: Interval[] = [];

  for (const part of value.split(',')) {
    const [from, to] = part.split('-');
    const start = toMinutes(from ?? '');
    const end = toMinutes(to ?? '');
    if (start === null || end === null) {
      return null;
    }
    intervals.push({ start, end });
  }

  return intervals;
}

/** Horario semanal, o null si el texto no se entiende. */
export function parseOpeningHours(value: string | undefined): Schedule | null {
  const text = value?.trim();
  if (!text) {
    return null;
  }

  const schedule: Schedule = ALL_DAYS.map(() => []);

  if (text === '24/7') {
    return schedule.map(() => [{ start: 0, end: 24 * 60 }]);
  }

  let understood = false;

  // «;» separa reglas en OSM; «·» en el formato de los datos de ejemplo.
  for (const rawRule of text.split(/[;·]/)) {
    const rule = rawRule.trim();
    if (!rule) {
      continue;
    }

    // Festivos (PH) y vacaciones escolares (SH) no afectan a un día normal.
    if (/^(ph|sh)\b/i.test(rule)) {
      continue;
    }

    const match = /^(?:([\p{L},\- ]+?)\s+)?(off|closed|cerrado|[\d:,\- ]+)$/iu.exec(rule);
    if (!match) {
      return null;
    }

    const days = match[1] ? parseDays(match[1].replace(/\s+/g, '')) : ALL_DAYS;
    if (!days) {
      return null;
    }

    const closed = /^(off|closed|cerrado)$/i.test(match[2]);
    const intervals = closed ? [] : parseTimes(match[2].replace(/\s+/g, ''));
    if (!intervals) {
      return null;
    }

    // En OSM una regla posterior sustituye a las anteriores para esos días.
    for (const day of days) {
      schedule[day] = intervals;
    }
    understood = true;
  }

  return understood ? schedule : null;
}

/**
 * ¿Está abierto en ese momento? `true`/`false`, o `null` si el horario es
 * desconocido o no se ha podido interpretar.
 */
export function isOpenAt(value: string | undefined, date: Date = new Date()): boolean | null {
  const schedule = parseOpeningHours(value);
  if (!schedule) {
    return null;
  }

  const day = (date.getDay() + 6) % 7;
  const previous = (day + 6) % 7;
  const minutes = date.getHours() * 60 + date.getMinutes();

  const openToday = schedule[day].some(({ start, end }) =>
    end > start ? minutes >= start && minutes < end : minutes >= start,
  );

  // Tramos de ayer que pasan de medianoche (p. ej. 20:00-02:00).
  const openFromYesterday = schedule[previous].some(
    ({ start, end }) => end <= start && minutes < end,
  );

  return openToday || openFromYesterday;
}
