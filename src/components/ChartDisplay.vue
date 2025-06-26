<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import WordCloud from 'wordcloud'
import * as XLSX from 'xlsx'

interface Props {
  data: any[]
  title?: string
}

const props = defineProps<Props>()

type ChartType = 'column' | 'bar' | 'line' | 'pie' | 'wordcloud'

const selectedChartType = ref<ChartType>('column')
const selectedXAxis = ref('')
const selectedYAxis = ref('')
const showChart = ref(false)
const wordCloudCanvas = ref<HTMLCanvasElement>()
const isExporting = ref(false)

// Simple chart types - only 5 essential ones
const chartTypes = [
  { value: 'column', label: 'Column', icon: '📊' },
  { value: 'bar', label: 'Bar', icon: '📈' },
  { value: 'line', label: 'Line', icon: '📉' },
  { value: 'pie', label: 'Pie', icon: '🥧' },
  { value: 'wordcloud', label: 'Word Cloud', icon: '☁️' }
]

// Get available columns from data
const availableColumns = computed(() => {
  if (!props.data || props.data.length === 0) return []
  return Object.keys(props.data[0])
})

// Get numeric columns for Y-axis
const numericColumns = computed(() => {
  if (!props.data || props.data.length === 0) return []
  
  const firstRow = props.data[0]
  return Object.keys(firstRow).filter(key => {
    const value = firstRow[key]
    return typeof value === 'number' || 
           (typeof value === 'string' && !isNaN(Number(value)) && value !== '')
  })
})

// Get text columns for Word Cloud
const textColumns = computed(() => {
  if (!props.data || props.data.length === 0) return []
  
  const firstRow = props.data[0]
  return Object.keys(firstRow).filter(key => {
    const value = firstRow[key]
    return typeof value === 'string' && isNaN(Number(value))
  })
})

// Auto-detect best columns for charting
watch(() => props.data, (newData) => {
  if (newData && newData.length > 0) {
    // Auto-select X-axis (first non-numeric column or first column)
    const nonNumericCols = availableColumns.value.filter(col => !numericColumns.value.includes(col))
    selectedXAxis.value = nonNumericCols.length > 0 ? nonNumericCols[0] : availableColumns.value[0]
    
    // Auto-select Y-axis (first numeric column)
    selectedYAxis.value = numericColumns.value.length > 0 ? numericColumns.value[0] : availableColumns.value[1] || availableColumns.value[0]
  }
}, { immediate: true })

// Process data for Word Cloud
const processWordCloudData = () => {
  if (!props.data || !selectedXAxis.value) return []
  
  // Count frequency of words/terms
  const wordFreq = new Map()
  
  props.data.forEach(row => {
    const text = String(row[selectedXAxis.value] || '').toLowerCase()
    
    // Split text into words and clean them
    const words = text
      .replace(/[^\w\s]/g, ' ') // Remove punctuation
      .split(/\s+/)
      .filter(word => word.length > 2) // Filter out short words
      .filter(word => !['the', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 'of', 'with', 'by', 'from', 'up', 'about', 'into', 'through', 'during', 'before', 'after', 'above', 'below', 'between', 'among', 'is', 'are', 'was', 'were', 'be', 'been', 'being', 'have', 'has', 'had', 'do', 'does', 'did', 'will', 'would', 'could', 'should', 'may', 'might', 'must', 'can', 'this', 'that', 'these', 'those', 'i', 'you', 'he', 'she', 'it', 'we', 'they', 'me', 'him', 'her', 'us', 'them', 'my', 'your', 'his', 'her', 'its', 'our', 'their', 'a', 'an'].includes(word)) // Remove common stop words
    
    words.forEach(word => {
      wordFreq.set(word, (wordFreq.get(word) || 0) + 1)
    })
  })
  
  // Convert to array format for WordCloud library
  return Array.from(wordFreq.entries())
    .sort((a, b) => b[1] - a[1]) // Sort by frequency
    .slice(0, 100) // Limit to top 100 words
}

// Generate Word Cloud
const generateWordCloud = async () => {
  if (!wordCloudCanvas.value || selectedChartType.value !== 'wordcloud') return
  
  const wordData = processWordCloudData()
  if (wordData.length === 0) return
  
  // Clear canvas
  const ctx = wordCloudCanvas.value.getContext('2d')
  if (ctx) {
    ctx.clearRect(0, 0, wordCloudCanvas.value.width, wordCloudCanvas.value.height)
  }
  
  // Generate word cloud with bigger size settings
  WordCloud(wordCloudCanvas.value, {
    list: wordData,
    gridSize: Math.round(12 * wordCloudCanvas.value.width / 1200), // Smaller grid for more words
    weightFactor: function (size) {
      return Math.pow(size, 0.9) * wordCloudCanvas.value.width / 800 * 25 // Bigger weight factor
    },
    fontFamily: 'Inter, system-ui, sans-serif',
    color: function () {
      const colors = ['#3B82F6', '#EF4444', '#10B981', '#F59E0B', '#8B5CF6', '#06B6D4', '#F97316', '#84CC16', '#EC4899', '#6366F1']
      return colors[Math.floor(Math.random() * colors.length)]
    },
    rotateRatio: 0.4, // More rotation variety
    rotationSteps: 3,
    backgroundColor: 'transparent',
    minSize: 12, // Bigger minimum size
    maxSize: 80, // Set maximum size
    drawOutOfBound: false,
    shrinkToFit: true,
    click: function(item) {
      console.log('Word clicked:', item[0], 'Frequency:', item[1])
    }
  })
}

// Watch for word cloud generation
watch([selectedChartType, selectedXAxis], async () => {
  if (selectedChartType.value === 'wordcloud') {
    await nextTick()
    generateWordCloud()
  }
})

// Process data for Highcharts - YOUR EXACT WORKING CODE
const processedData = computed(() => {
  if (!props.data || !selectedXAxis.value || !selectedYAxis.value) {
    return { categories: [], series: [] }
  }

  // Group data by X-axis value and aggregate Y-axis values
  const groupedData = new Map()
  
  props.data.forEach(row => {
    const xValue = String(row[selectedXAxis.value])
    const yValue = Number(row[selectedYAxis.value]) || 0
    
    if (groupedData.has(xValue)) {
      groupedData.set(xValue, groupedData.get(xValue) + yValue)
    } else {
      groupedData.set(xValue, yValue)
    }
  })

  const categories = Array.from(groupedData.keys())
  const values = Array.from(groupedData.values())

  // For pie charts, format data differently
  if (selectedChartType.value === 'pie') {
    const pieData = categories.map((category, index) => ({
      name: category,
      y: values[index]
    }))
    
    return {
      categories: [],
      series: [{
        type: 'pie',
        name: selectedYAxis.value,
        data: pieData
      }]
    }
  }

  // For other chart types
  return {
    categories,
    series: [{
      type: selectedChartType.value,
      name: selectedYAxis.value,
      data: values
    }]
  }
})

// Highcharts configuration - YOUR EXACT WORKING CODE with centered alignment
const chartOptions = computed(() => {
  const isPieChart = selectedChartType.value === 'pie'
  
  return {
    chart: {
      type: selectedChartType.value,
      backgroundColor: 'transparent',
      height: 500, // Increased height for better visibility
      alignTicks: false,
      plotBorderWidth: 0,
      spacingTop: 20,
      spacingBottom: 20,
      spacingLeft: 20,
      spacingRight: 20
    },
    title: {
      text: props.title || `${selectedYAxis.value} by ${selectedXAxis.value}`,
      align: 'center', // Center the title
      style: {
        fontSize: '18px', // Slightly bigger title
        fontWeight: 'bold',
        color: '#1e293b'
      }
    },
    xAxis: isPieChart ? undefined : {
      categories: processedData.value.categories,
      title: {
        text: selectedXAxis.value,
        align: 'middle',
        style: { fontWeight: 'bold' }
      }
    },
    yAxis: isPieChart ? undefined : {
      title: {
        text: selectedYAxis.value,
        align: 'middle',
        style: { fontWeight: 'bold' }
      }
    },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.85)',
      style: { color: 'white' },
      borderRadius: 8,
      formatter: function() {
        if (isPieChart) {
          return `<b>${this.point.name}</b><br/>${this.series.name}: ${this.y}<br/>Percentage: ${this.percentage.toFixed(1)}%`
        } else {
          return `<b>${this.x}</b><br/>${this.series.name}: ${this.y}`
        }
      }
    },
    plotOptions: {
      series: {
        cursor: 'pointer',
        point: {
          events: {
            click: function() {
              console.log('Point clicked:', this.name || this.category, this.y)
            }
          }
        }
      },
      pie: {
        allowPointSelect: true,
        cursor: 'pointer',
        center: ['50%', '50%'], // Center the pie chart
        size: '80%', // Make pie chart bigger
        dataLabels: {
          enabled: true,
          format: '<b>{point.name}</b>: {point.percentage:.1f}%'
        },
        showInLegend: true
      },
      column: {
        colorByPoint: true,
        pointPadding: 0.1,
        groupPadding: 0.1
      },
      bar: {
        colorByPoint: true,
        pointPadding: 0.1,
        groupPadding: 0.1
      }
    },
    legend: {
      enabled: isPieChart,
      align: 'center',
      verticalAlign: 'bottom'
    },
    colors: [
      '#3B82F6', '#EF4444', '#10B981', '#F59E0B', '#8B5CF6',
      '#06B6D4', '#F97316', '#84CC16', '#EC4899', '#6366F1'
    ],
    series: processedData.value.series,
    credits: { enabled: false },
    exporting: { enabled: true }
  }
})

const toggleChart = () => {
  showChart.value = !showChart.value
}

const canCreateChart = computed(() => {
  return props.data && props.data.length > 0 && selectedXAxis.value && 
         (selectedChartType.value === 'wordcloud' || selectedYAxis.value)
})

// Word cloud statistics
const wordCloudStats = computed(() => {
  if (selectedChartType.value !== 'wordcloud') return null
  
  const wordData = processWordCloudData()
  return {
    totalWords: wordData.length,
    mostFrequent: wordData[0] || ['N/A', 0],
    totalOccurrences: wordData.reduce((sum, [, freq]) => sum + freq, 0)
  }
})

// Excel Export Functions
const exportChartDataToExcel = () => {
  if (!props.data || props.data.length === 0) {
    alert('No data to export!')
    return
  }

  try {
    isExporting.value = true

    // Create a new workbook
    const workbook = XLSX.utils.book_new()
    
    let exportData = []
    let sheetName = 'Chart Data'

    if (selectedChartType.value === 'wordcloud') {
      // Export word cloud data
      const wordData = processWordCloudData()
      exportData = wordData.map(([word, frequency]) => ({
        Word: word,
        Frequency: frequency
      }))
      sheetName = 'Word Cloud Data'
    } else {
      // Export chart data (processed/aggregated)
      const categories = processedData.value.categories
      const values = processedData.value.series[0]?.data || []
      
      if (selectedChartType.value === 'pie') {
        exportData = values.map(item => ({
          Category: item.name,
          Value: item.y
        }))
      } else {
        exportData = categories.map((category, index) => ({
          [selectedXAxis.value]: category,
          [selectedYAxis.value]: values[index] || 0
        }))
      }
    }
    
    // Convert data to worksheet
    const worksheet = XLSX.utils.json_to_sheet(exportData)
    
    // Auto-size columns
    const columnWidths = []
    if (exportData.length > 0) {
      const headers = Object.keys(exportData[0])
      headers.forEach((header) => {
        let maxWidth = header.length
        exportData.forEach(row => {
          const cellValue = String(row[header] || '')
          maxWidth = Math.max(maxWidth, cellValue.length)
        })
        columnWidths.push({ wch: Math.min(maxWidth + 2, 50) })
      })
      worksheet['!cols'] = columnWidths
    }

    // Add worksheet to workbook
    XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)
    
    // Generate filename with timestamp and chart type
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[:.]/g, '-')
    const chartType = chartTypes.find(t => t.value === selectedChartType.value)?.label || 'Chart'
    const filename = `${chartType.toLowerCase()}-data-${timestamp}.xlsx`
    
    // Save the file
    XLSX.writeFile(workbook, filename)
    
    console.log(`Excel file exported: ${filename}`)
  } catch (error) {
    console.error('Error exporting chart data to Excel:', error)
    alert('Failed to export chart data to Excel. Please try again.')
  } finally {
    isExporting.value = false
  }
}

const exportOriginalDataToExcel = () => {
  if (!props.data || props.data.length === 0) {
    alert('No data to export!')
    return
  }

  try {
    isExporting.value = true

    // Create a new workbook
    const workbook = XLSX.utils.book_new()
    
    // Convert original data to worksheet
    const worksheet = XLSX.utils.json_to_sheet(props.data)
    
    // Auto-size columns
    const columnWidths = []
    if (props.data.length > 0) {
      const headers = Object.keys(props.data[0])
      headers.forEach((header) => {
        let maxWidth = header.length
        props.data.forEach(row => {
          const cellValue = String(row[header] || '')
          maxWidth = Math.max(maxWidth, cellValue.length)
        })
        columnWidths.push({ wch: Math.min(maxWidth + 2, 50) })
      })
      worksheet['!cols'] = columnWidths
    }

    // Add worksheet to workbook
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Original Data')
    
    // Generate filename with timestamp
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[:.]/g, '-')
    const filename = `original-data-${timestamp}.xlsx`
    
    // Save the file
    XLSX.writeFile(workbook, filename)
    
    console.log(`Excel file exported: ${filename}`)
  } catch (error) {
    console.error('Error exporting original data to Excel:', error)
    alert('Failed to export original data to Excel. Please try again.')
  } finally {
    isExporting.value = false
  }
}
</script>

<template>
  <div class="card">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-xl font-semibold text-slate-800">📊 Charts</h3>
        <p class="text-sm text-slate-600 mt-1">Visualize your data with interactive charts</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- Excel Export Dropdown -->
        <div v-if="canCreateChart" class="relative group">
          <button class="btn-primary text-sm flex items-center" :disabled="isExporting">
            <svg v-if="!isExporting" class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-4-4m4 4l4-4m-6 8h8a2 2 0 002-2V7a2 2 0 00-2-2H8a2 2 0 00-2 2v11a2 2 0 002 2z"/>
            </svg>
            <div v-else class="loading-spinner mr-2"></div>
            {{ isExporting ? 'Exporting...' : 'Export Excel' }}
            <svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
            </svg>
          </button>
          
          <!-- Dropdown Menu -->
          <div class="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-slate-200 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 z-10">
            <div class="py-2">
              <button
                @click="exportChartDataToExcel"
                :disabled="isExporting"
                class="w-full text-left px-4 py-2 text-sm text-slate-700 hover:bg-green-50 hover:text-green-700 flex items-center transition-colors"
              >
                <svg class="w-4 h-4 mr-3 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                </svg>
                📊 Export Chart Data
                <span class="text-xs text-slate-500 ml-auto">Processed</span>
              </button>
              
              <button
                @click="exportOriginalDataToExcel"
                :disabled="isExporting"
                class="w-full text-left px-4 py-2 text-sm text-slate-700 hover:bg-blue-50 hover:text-blue-700 flex items-center transition-colors"
              >
                <svg class="w-4 h-4 mr-3 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
                📄 Export Original Data
                <span class="text-xs text-slate-500 ml-auto">Raw</span>
              </button>
            </div>
          </div>
        </div>

        <button
          @click="toggleChart"
          :disabled="!canCreateChart"
          class="btn-primary"
          :class="{ 'opacity-50 cursor-not-allowed': !canCreateChart }"
        >
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
          </svg>
          {{ showChart ? 'Hide Chart' : 'Create Chart' }}
        </button>
      </div>
    </div>

    <div v-if="!canCreateChart" class="text-center py-12 text-slate-500">
      <svg class="w-16 h-16 mx-auto mb-4 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
      </svg>
      <p class="text-lg font-medium mb-2">No Data Available</p>
      <p>Run a query first to generate charts from your data</p>
    </div>

    <Transition name="slide">
      <div v-if="showChart && canCreateChart" class="space-y-6">
        <!-- Chart Type Selection -->
        <div class="flex flex-wrap gap-2 justify-center">
          <button
            v-for="type in chartTypes"
            :key="type.value"
            @click="selectedChartType = type.value"
            class="flex items-center px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200"
            :class="selectedChartType === type.value 
              ? 'bg-blue-500 text-white shadow-md' 
              : 'bg-white text-slate-700 hover:bg-blue-100 border border-slate-200'"
          >
            <span class="mr-2 text-lg">{{ type.icon }}</span>
            {{ type.label }}
          </button>
        </div>

        <!-- Axis Selection for Regular Charts -->
        <div v-if="selectedChartType !== 'wordcloud'" class="grid grid-cols-1 md:grid-cols-2 gap-4 p-4 bg-slate-50 rounded-lg">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-2">X-Axis (Categories)</label>
            <select v-model="selectedXAxis" class="input-field text-sm">
              <option v-for="column in availableColumns" :key="column" :value="column">
                {{ column }}
              </option>
            </select>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-2">Y-Axis (Values)</label>
            <select v-model="selectedYAxis" class="input-field text-sm">
              <option v-for="column in availableColumns" :key="column" :value="column">
                {{ column }}
              </option>
            </select>
          </div>
        </div>

        <!-- Word Cloud Configuration -->
        <div v-if="selectedChartType === 'wordcloud'" class="p-4 bg-slate-50 rounded-lg">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-2">Text Column for Word Cloud</label>
            <select v-model="selectedXAxis" @change="generateWordCloud" class="input-field text-sm">
              <optgroup label="Text Columns" v-if="textColumns.length > 0">
                <option v-for="column in textColumns" :key="column" :value="column">
                  {{ column }}
                </option>
              </optgroup>
              <optgroup label="All Columns">
                <option v-for="column in availableColumns" :key="column" :value="column">
                  {{ column }}
                </option>
              </optgroup>
            </select>
            <p class="text-xs text-slate-500 mt-1">
              💡 Word clouds work best with text columns containing multiple words
            </p>
          </div>
        </div>

        <!-- Chart Display - CENTERED AND BIGGER -->
        <div class="bg-white p-6 rounded-lg border border-slate-200 shadow-sm">
          <!-- Highcharts for regular charts - CENTERED -->
          <div v-if="selectedChartType !== 'wordcloud'" class="flex justify-center">
            <div class="w-full max-w-5xl">
              <highcharts 
                :options="chartOptions"
                class="w-full"
              />
            </div>
          </div>
          
          <!-- Canvas for Word Cloud - BIGGER AND CENTERED -->
          <div v-if="selectedChartType === 'wordcloud'" class="text-center">
            <h4 class="text-xl font-semibold text-slate-800 mb-6">
              {{ props.title || `Word Cloud: ${selectedXAxis}` }}
            </h4>
            <div class="flex justify-center">
              <canvas 
                ref="wordCloudCanvas"
                width="1200"
                height="600"
                class="max-w-full h-auto border-2 border-slate-300 rounded-xl bg-gradient-to-br from-white to-slate-50 cursor-pointer shadow-lg hover:shadow-xl transition-shadow duration-300"
                @click="generateWordCloud"
                style="max-height: 600px;"
              />
            </div>
            <div class="mt-4 space-y-2">
              <p class="text-sm text-slate-600 font-medium">
                🖱️ Click on words to see details in console • Click canvas to regenerate
              </p>
              <button 
                @click="generateWordCloud"
                class="btn-secondary text-sm"
              >
                🔄 Regenerate Word Cloud
              </button>
            </div>
          </div>
        </div>

        <!-- Chart Info -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <!-- Regular Chart Stats -->
          <template v-if="selectedChartType !== 'wordcloud'">
            <div class="p-4 bg-blue-50 border border-blue-200 rounded-lg text-center">
              <p class="text-sm font-medium text-blue-800">Data Points</p>
              <p class="text-2xl font-bold text-blue-600">{{ processedData.categories.length || processedData.series[0]?.data?.length || 0 }}</p>
            </div>

            <div class="p-4 bg-green-50 border border-green-200 rounded-lg text-center">
              <p class="text-sm font-medium text-green-800">Chart Type</p>
              <p class="text-lg font-bold text-green-600">{{ chartTypes.find(t => t.value === selectedChartType)?.label }}</p>
            </div>

            <div class="p-4 bg-purple-50 border border-purple-200 rounded-lg text-center">
              <p class="text-sm font-medium text-purple-800">Visualization</p>
              <p class="text-sm font-bold text-purple-600">{{ selectedXAxis }} vs {{ selectedYAxis }}</p>
            </div>
          </template>

          <!-- Word Cloud Stats -->
          <template v-if="selectedChartType === 'wordcloud' && wordCloudStats">
            <div class="p-4 bg-blue-50 border border-blue-200 rounded-lg text-center">
              <p class="text-sm font-medium text-blue-800">Unique Words</p>
              <p class="text-2xl font-bold text-blue-600">{{ wordCloudStats.totalWords }}</p>
            </div>

            <div class="p-4 bg-green-50 border border-green-200 rounded-lg text-center">
              <p class="text-sm font-medium text-green-800">Most Frequent</p>
              <p class="text-lg font-bold text-green-600">{{ wordCloudStats.mostFrequent[0] }}</p>
              <p class="text-xs text-green-600">({{ wordCloudStats.mostFrequent[1] }} times)</p>
            </div>

            <div class="p-4 bg-purple-50 border border-purple-200 rounded-lg text-center">
              <p class="text-sm font-medium text-purple-800">Total Words</p>
              <p class="text-2xl font-bold text-purple-600">{{ wordCloudStats.totalOccurrences }}</p>
            </div>
          </template>
        </div>

        <!-- Export Info Banner -->
        <div v-if="canCreateChart" class="p-4 bg-gradient-to-r from-green-50 to-blue-50 border border-green-200 rounded-lg">
          <div class="flex items-start">
            <svg class="w-5 h-5 text-green-500 mt-0.5 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-4-4m4 4l4-4m-6 8h8a2 2 0 002-2V7a2 2 0 00-2-2H8a2 2 0 00-2 2v11a2 2 0 002 2z"/>
            </svg>
            <div class="text-sm text-green-800">
              <p class="font-medium mb-1">📊 Export Chart Data to Excel</p>
              <p>Use the <strong>Export Excel</strong> button above to download:</p>
              <ul class="mt-1 space-y-1 text-xs">
                <li>• <strong>Chart Data:</strong> Processed/aggregated data used in the chart</li>
                <li>• <strong>Original Data:</strong> Raw query results before processing</li>
                <li>• <strong>Word Cloud Data:</strong> Word frequencies when using word cloud</li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Simple Tips -->
        <div class="p-4 bg-amber-50 border border-amber-200 rounded-lg">
          <div class="flex items-start">
            <span class="text-2xl mr-3">💡</span>
            <div class="text-sm text-amber-800">
              <p class="font-semibold mb-2">Chart Tips:</p>
              <ul class="space-y-1">
                <li>• <strong>Column:</strong> Compare categories</li>
                <li>• <strong>Bar:</strong> Horizontal comparison</li>
                <li>• <strong>Line:</strong> Show trends over time</li>
                <li>• <strong>Pie:</strong> Show proportions</li>
                <li>• <strong>Word Cloud:</strong> Visualize text frequency (bigger = more frequent)</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>