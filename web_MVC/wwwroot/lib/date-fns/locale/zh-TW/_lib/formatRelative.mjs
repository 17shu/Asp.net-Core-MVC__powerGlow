const formatRelativeLocale = {
  lastWeek: "'上個'eeee p",
  yesterday: "'昨天' p",
  today: "'今天' p",
  tomorrow: "'明天' p",
  nextWeek: "'下個'eeee p",
  other: "P",
};

export const formatRelative = (token, _date, _baseDate, _options) =>
  formatRelativeLocale[token];
