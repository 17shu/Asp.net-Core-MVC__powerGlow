function futureSeconds(text) {
  return text.replace(/sekuntia?/, "sekunnin");
}

function futureMinutes(text) {
  return text.replace(/minuuttia?/, "minuutin");
}

function futureHours(text) {
  return text.replace(/tuntia?/, "tunnin");
}

function futureDays(text) {
  return text.replace(/päivää?/, "päivän");
}

function futureWeeks(text) {
  return text.replace(/(viikko|viikkoa)/, "viikon");
}

function futureMonths(text) {
  return text.replace(/(kuukausi|kuukautta)/, "kuukauden");
}

function futureYears(text) {
  return text.replace(/(vuosi|vuotta)/, "vuoden");
}

const formatDistanceLocale = {
  lessThanXSeconds: {
    one: "alle sekunti",
    other: "alle {{count}} sekuntia",
    futureTense: futureSeconds,
  },

  xSeconds: {
    one: "sekunti",
    other: "{{count}} sekuntia",
    futureTense: futureSeconds,
  },

  halfAMinute: {
    one: "puoli minuuttia",
    other: "puoli minuuttia",
    futureTense: (_text) => "puolen minuutin",
  },

  lessThanXMinutes: {
    one: "alle minuutti",
    other: "alle {{count}} minuuttia",
    futureTense: futureMinutes,
  },

  xMinutes: {
    one: "minuutti",
    other: "{{count}} minuuttia",
    futureTense: futureMinutes,
  },

  aboutXHours: {
    one: "noin tunti",
    other: "noin {{count}} tuntia",
    futureTense: futureHours,
  },

  xHours: {
    one: "tunti",
    other: "{{count}} tuntia",
    futureTense: futureHours,
  },

  xDays: {
    one: "päivä",
    other: "{{count}} päivää",
    futureTense: futureDays,
  },

  aboutXWeeks: {
    one: "noin viikko",
    other: "noin {{count}} viikkoa",
    futureTense: futureWeeks,
  },

  xWeeks: {
    one: "viikko",
    other: "{{count}} viikkoa",
    futureTense: futureWeeks,
  },

  aboutXMonths: {
    one: "noin kuukausi",
    other: "noin {{count}} kuukautta",
    futureTense: futureMonths,
  },

  xMonths: {
    one: "kuukausi",
    other: "{{count}} kuukautta",
    futureTense: futureMonths,
  },

  aboutXYears: {
    one: "noin vuosi",
    other: "noin {{count}} vuotta",
    futureTense: futureYears,
  },

  xYears: {
    one: "vuosi",
    other: "{{count}} vuotta",
    futureTense: futureYears,
  },

  overXYears: {
    one: "yli vuosi",
    other: "yli {{count}} vuotta",
    futureTense: futureYears,
  },

  almostXYears: {
    one: "lähes vuosi",
    other: "lähes {{count}} vuotta",
    futureTense: futureYears,
  },
};

export const formatDistance = (token, count, options) => {
  const tokenValue = formatDistanceLocale[token];
  const result =
    count === 1
      ? tokenValue.one
      : tokenValue.other.replace("{{count}}", String(count));

  if (options?.addSuffix) {
    if (options.comparison && options.comparison > 0) {
      return tokenValue.futureTense(result) + " kuluttua";
    } else {
      return result + " sitten";
    }
  }

  return result;
};
