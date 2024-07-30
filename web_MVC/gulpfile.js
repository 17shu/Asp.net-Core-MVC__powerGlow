const gulp = require('gulp');
const concat = require('gulp-concat');
const uglify = require('gulp-uglify');
const rename = require('gulp-rename');
const cleanCSS = require('gulp-clean-css');

const paths = {
    scripts: {
        src: [
            'node_modules/chart.js/dist/chart.umd.js', // �T�{�o�Ӥ��s�b
            'node_modules/chartjs-adapter-date-fns/dist/chartjs-adapter-date-fns.bundle.js', // �T�{�o�Ӥ��s�b
            'node_modules/date-fns/index.js' // �ϥ� index.js
        ],
        dest: 'wwwroot/lib'
    }
};

// �ƻs JS ���� wwwroot/lib
function scripts() {
    return gulp.src(paths.scripts.src, { allowEmpty: true })
        .on('error', function (err) { console.error('Error!', err.message); })
        .pipe(gulp.dest(paths.scripts.dest));
}

exports.scripts = scripts;
exports.default = gulp.series(scripts);
