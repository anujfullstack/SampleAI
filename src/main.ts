import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import HighchartsVue from 'highcharts-vue'
import Highcharts from 'highcharts'

// Import additional Highcharts modules
import HighchartsMore from 'highcharts/highcharts-more'
import HighchartsExporting from 'highcharts/modules/exporting'

// Initialize Highcharts modules
// HighchartsMore(Highcharts)
// HighchartsExporting(Highcharts)

const app = createApp(App)
app.use(HighchartsVue)
app.mount('#app')