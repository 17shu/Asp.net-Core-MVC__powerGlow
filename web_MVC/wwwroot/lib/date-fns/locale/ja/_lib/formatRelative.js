"use strict";
exports.formatRelative = void 0;

const formatRelativeLocale = {
  lastWeek: "先週のeeeeのp",
  yesterday: "昨日のp",
  today: "今日のp",
  tomorrow: "明日のp",
  nextWeek: "翌週のeeeeのp",
  other: "P",
};

const formatRelative = (token, _date, _baseDate, _options) => {
  return formatRelativeLocale[token];
};
exports.formatRelative = formatRelative;
